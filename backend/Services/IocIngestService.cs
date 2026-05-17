using backend.Models;
using backend.Repositories;
using backend.DTOs;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;

namespace backend.Services
{
    public class IocIngestService 
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IocIngestRepository _iocIngestRepository;
        private readonly ILogger<IocIngestService> _logger;

        public IocIngestService(
            IHttpClientFactory httpClientFactory,
            IocIngestRepository iocIngestRepository,
            ILogger<IocIngestService> logger)
        {
            _httpClientFactory = httpClientFactory;
            _iocIngestRepository = iocIngestRepository;
            _logger = logger;
        }

        private string GenerateDeterministicKey(string InputString)
        {
            using (var Sha256Hash = System.Security.Cryptography.SHA256.Create())
            {
                var BytesArray = System.Text.Encoding.UTF8.GetBytes(InputString.ToLowerInvariant().Trim());
                var ComputedHash = Sha256Hash.ComputeHash(BytesArray);
                return BitConverter.ToString(ComputedHash).Replace("-", "").ToLower();
            }
        }

        private int CalculateDynamicRiskScore(OtxPulse PulseData, OtxIndicator IndicatorData)
        {
            int ScoreValue = 0;
            if (IndicatorData.IsActive == 1) ScoreValue += 20; else ScoreValue += 5;
            if (IndicatorData.Expiration.HasValue && IndicatorData.Expiration.Value < DateTime.UtcNow) ScoreValue -= 15;
            if (IndicatorData.Observations > 100) ScoreValue += 15;
            else if (IndicatorData.Observations > 10) ScoreValue += 10;
            else if (IndicatorData.Observations > 0) ScoreValue += 5;

            string RoleString = (IndicatorData.Role ?? "").ToLower();
            if (RoleString.Contains("c2") || RoleString.Contains("malware") || RoleString.Contains("phishing") || RoleString.Contains("exploit")) ScoreValue += 10;

            string TypeString = (IndicatorData.Type ?? "").ToLower();
            if (TypeString.Contains("hash") || TypeString.Contains("md5") || TypeString.Contains("sha")) ScoreValue += 10;
            else if (TypeString.Contains("domain") || TypeString.Contains("hostname")) ScoreValue += 7;
            else if (TypeString.Contains("ipv4") || TypeString.Contains("ipv6")) ScoreValue += 5;

            string TlpLevel = (PulseData.TLP ?? "").ToLower();
            if (TlpLevel == "red") ScoreValue += 20;
            else if (TlpLevel == "amber") ScoreValue += 15;
            else if (TlpLevel == "green") ScoreValue += 10;
            else if (TlpLevel == "white") ScoreValue += 5;

            if (!string.IsNullOrWhiteSpace(PulseData.Adversary)) ScoreValue += 15;
            if (PulseData.TargetedCountries != null && PulseData.TargetedCountries.Count > 0) ScoreValue += 5;
            if (PulseData.Industries != null && PulseData.Industries.Count > 0) ScoreValue += 5;

            if (PulseData.References != null)
            {
                if (PulseData.References.Count > 2) ScoreValue += 10;
                else if (PulseData.References.Count > 0) ScoreValue += 5;
            }
            if (PulseData.Revision > 5) ScoreValue += 5;

            return Math.Max(1, Math.Min(100, ScoreValue));
        }

        public async Task<int> SyncAlienVaultDataAsync()
        {
            var HttpClientInstance = _httpClientFactory.CreateClient("AlienVaultClient");
            int TotalAddedRecords = 0;
            string CurrentPageUrl = "pulses/subscribed?limit=15"; 

            try
            {
                while (!string.IsNullOrEmpty(CurrentPageUrl))
                {
                    var ApiResponse = await HttpClientInstance.GetFromJsonAsync<OtxPulseResponse>(CurrentPageUrl);
                    
                    if (ApiResponse?.Results == null || !ApiResponse.Results.Any()) 
                    {
                        break;
                    }

                    var NodesDictionary = new Dictionary<string, IocNode>();
                    var EdgesDictionary = new Dictionary<string, object>();

                    foreach (var PulseItem in ApiResponse.Results)
                    {
                        string CampaignIdString = $"Campaign_{PulseItem.Id}";
                        string CampaignKey = GenerateDeterministicKey(CampaignIdString);
                        string PulseName = string.IsNullOrWhiteSpace(PulseItem.Name) ? CampaignIdString : PulseItem.Name;

                        NodesDictionary[CampaignKey] = new IocNode
                        {
                            _key = CampaignKey, // ✅ Đã cập nhật thành _key
                            Type = "Campaign",
                            Value = PulseName,
                            RiskScore = 90,
                            OriginRef = "AlienVault OTX",
                            Tags = new List<string> { "Pulse" }
                        };

                        string? PreviousIocKey = null;
                        string? FirstIocKey = null;

                        foreach (var IndicatorItem in PulseItem.Indicators)
                        {
                            var MappedType = MapOtxTypeToSystemType(IndicatorItem.Type);
                            if (MappedType == null) continue;

                            string CleanedValue = IndicatorItem.Indicator.Trim();
                            string IocKey = GenerateDeterministicKey(CleanedValue);
                            int DynamicRiskScore = CalculateDynamicRiskScore(PulseItem, IndicatorItem);

                            if (!NodesDictionary.ContainsKey(IocKey))
                            {
                                NodesDictionary[IocKey] = new IocNode
                                {
                                    _key = IocKey, // ✅ Đã cập nhật thành _key
                                    Type = MappedType,
                                    Value = CleanedValue,
                                    RiskScore = DynamicRiskScore,
                                    OriginRef = "AlienVault OTX",
                                    Tags = new List<string> { "OTX", "AutoSync" }
                                };
                            }
                            else
                            {
                                NodesDictionary[IocKey].RiskScore = Math.Max(NodesDictionary[IocKey].RiskScore, DynamicRiskScore);
                            }

                            string BelongsToEdgeKey = $"{IocKey}_belongs_to_{CampaignKey}";
                            EdgesDictionary[BelongsToEdgeKey] = new
                            {
                                _from = $"IocNodes/{IocKey}",
                                _to = $"IocNodes/{CampaignKey}",
                                RelationType = "belongs_to",
                                OriginRef = "AlienVault AutoSync"
                            };

                            if (!string.IsNullOrEmpty(PreviousIocKey) && IocKey != PreviousIocKey)
                            {
                                string RelatedEdgeKey = $"{IocKey}_related_ioc_{PreviousIocKey}";
                                EdgesDictionary[RelatedEdgeKey] = new
                                {
                                    _from = $"IocNodes/{IocKey}",
                                    _to = $"IocNodes/{PreviousIocKey}",
                                    RelationType = "related_ioc"
                                };
                            }
                            
                            if (FirstIocKey == null) 
                            {
                                FirstIocKey = IocKey;
                            }
                            
                            PreviousIocKey = IocKey;

                            if (NodesDictionary.Count >= 2000)
                            {
                                await _iocIngestRepository.BulkUpsertNodesAsync(NodesDictionary.Values.ToList()); 
                                await _iocIngestRepository.BulkInsertEdgesAsync(EdgesDictionary.Values.ToList());
                                TotalAddedRecords += NodesDictionary.Count;

                                NodesDictionary.Clear();
                                EdgesDictionary.Clear();
                            }
                        }

                        if (!string.IsNullOrEmpty(PreviousIocKey) && !string.IsNullOrEmpty(FirstIocKey) && PreviousIocKey != FirstIocKey)
                        {
                            string CloseRingKey = $"{PreviousIocKey}_related_ioc_{FirstIocKey}";
                            EdgesDictionary[CloseRingKey] = new 
                            { 
                                _from = $"IocNodes/{PreviousIocKey}", 
                                _to = $"IocNodes/{FirstIocKey}", 
                                RelationType = "related_ioc" 
                            };
                        }
                    }

                    if (NodesDictionary.Any())
                    {
                        await _iocIngestRepository.BulkUpsertNodesAsync(NodesDictionary.Values.ToList());
                        await _iocIngestRepository.BulkInsertEdgesAsync(EdgesDictionary.Values.ToList());
                        TotalAddedRecords += NodesDictionary.Count;
                    }

                    CurrentPageUrl = ApiResponse.Next;
                }
            }
            catch (Exception ExceptionInstance)
            {
                _logger.LogError($"Data sync error: {ExceptionInstance.Message}");
            }

            _logger.LogInformation($"[Sync] Successfully ingested {TotalAddedRecords} IOCs using Batch Processing.");
            return TotalAddedRecords;
        }

        private string? MapOtxTypeToSystemType(string OtxType)
        {
            var TypeString = OtxType.ToLower();
            if (TypeString == "ipv4" || TypeString == "ipv6") return "IP";
            if (TypeString == "domain" || TypeString == "hostname") return "Domain";
            if (TypeString.Contains("hash") || TypeString == "md5" || TypeString == "sha1" || TypeString == "sha256") return "Hash";
            return null;
        }
    }
}