﻿using Microsoft.EntityFrameworkCore;
using DAL.Model;

namespace DAL
{
    public class OnBoardingAuthSkdDbContext : DbContext
    {
        public OnBoardingAuthSkdDbContext(DbContextOptions<OnBoardingAuthSkdDbContext> options) : base(options)
        {
        }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            //add index here
        }

        public DbSet<User> Users { get; set; }
        public DbSet<UserRole> UserRoles { get; set; }
        public DbSet<Role> Roles { get; set; }
    }
}
