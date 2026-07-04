// ==========================================
// System Includes
// ==========================================
global using System.Data;
global using System.Globalization;
global using System.IdentityModel.Tokens.Jwt;
global using System.Security.Claims;
global using System.Text;

// ==========================================
// Microsoft Includes
// ==========================================
global using Microsoft.AspNetCore.Authentication.JwtBearer;
global using Microsoft.AspNetCore.Authorization;
global using Microsoft.AspNetCore.DataProtection;
global using Microsoft.AspNetCore.Identity;
global using Microsoft.AspNetCore.Mvc;
global using Microsoft.EntityFrameworkCore;
global using Microsoft.IdentityModel.Tokens;
global using Microsoft.OpenApi.Models;

// ==========================================
// Third-Party Libraries
// ==========================================
global using FirebaseAdmin;
global using FluentValidation;
global using Google.Apis.Auth.OAuth2;
global using Hangfire;
global using Hangfire.Dashboard.BasicAuthorization;
global using Hangfire.SqlServer;
global using Serilog.Core;
global using Serilog.Events;
global using Serilog.Sinks.MSSqlServer;

// ==========================================
// Solution / Internal Projects
// ==========================================
global using AppExpenseTrackerApi.Middlewares;
global using AppExpenseTrackerApi.ViewModelValidator;
global using Domain.AppUser;
global using Domain.Interfaces;
global using ExpenseTrakcerHepler; // Note: Ensure spelling matches your actual namespace
global using Infrastructure;
global using Infrastructure.Data;
global using Infrastructure.Email;
global using Infrastructure.Email.Config;
global using Infrastructure.Repositories;
global using Services.BackgroundJobs;
global using Services.Interfaces;
global using Services.Management;
global using Services.Management.AuthService;
global using Services.ViewModels;
global using Services.ViewModels.ApiReponse;
global using Services.ViewModels.ApiViewModels;