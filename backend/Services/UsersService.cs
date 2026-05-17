using backend.Models;
using backend.Repositories;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace backend.Services
{
    public class UsersService
    {
        private readonly UsersRepository _usersRepository;

        public UsersService(UsersRepository usersRepository)
        {
            _usersRepository = usersRepository;
        }

        public async Task<IEnumerable<User>> GetAllUsersAsync()
        {
            return await _usersRepository.GetAllUsersAsync();
        }

        public async Task CreateUserAsync(User NewUser)
        {
            var ExistingUser = await _usersRepository.GetByUsernameAsync(NewUser.username);

            if (ExistingUser != null)
            {
                throw new InvalidOperationException($"Account '{NewUser.username}' already exists in the system. Please choose another username!");
            }

            string HashedPassword = BCrypt.Net.BCrypt.HashPassword(NewUser.password);
            
            var DocumentToInsert = new
            {
                username = NewUser.username,
                password = HashedPassword,
                role = string.IsNullOrEmpty(NewUser.role) ? "User" : NewUser.role,
                isLocked = false
            };

            await _usersRepository.CreateUserAsync(DocumentToInsert);
        }

        public async Task UpdateUserAsync(string Key, User UpdateData)
        {
            var ExistingUser = await _usersRepository.GetByUsernameExcludingKeyAsync(UpdateData.username, Key);

            if (ExistingUser != null)
            {
                throw new InvalidOperationException($"Username '{UpdateData.username}' is already taken by someone else!");
            }

            string UpdateQuery;
            
            var BindVars = new Dictionary<string, object>
            {
                { "key", Key },
                { "role", string.IsNullOrEmpty(UpdateData.role) ? "User" : UpdateData.role },
                { "isLocked", UpdateData.isLocked ?? false }
            };

            if (!string.IsNullOrWhiteSpace(UpdateData.password))
            {
                UpdateQuery = "UPDATE @key WITH { password: @password, role: @role, isLocked: @isLocked } IN Users";
                BindVars.Add("password", BCrypt.Net.BCrypt.HashPassword(UpdateData.password));
            }
            else
            {
                UpdateQuery = "UPDATE @key WITH { role: @role, isLocked: @isLocked } IN Users";
            }

            await _usersRepository.UpdateUserAsync(UpdateQuery, BindVars);
        }

        public async Task DeleteUserAsync(string Key, string CurrentUsername)
        {
            var UserToDelete = await _usersRepository.GetByKeyAsync(Key);

            if (UserToDelete == null)
            {
                throw new KeyNotFoundException("Account not found for deletion!");
            }

            if (UserToDelete.username == CurrentUsername)
            {
                throw new InvalidOperationException("Error: You cannot delete your own account!");
            }

            await _usersRepository.DeleteUserAsync(Key);
        }
    }
}