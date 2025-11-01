global using System.IdentityModel.Tokens.Jwt;
global using System.Security.Claims;
global using System.Text;
global using System.Globalization;

global using Microsoft.AspNetCore.Authorization;
global using Microsoft.IdentityModel.Tokens;
global using Microsoft.AspNetCore.Identity;
global using Microsoft.AspNetCore.Mvc;

global using Services.Interfaces;
global using Services.ViewModels;
global using Services.ViewModels.ApiViewModels;
global using Services.ViewModels.ApiReponse;

global using Domain.AppUser;
global using ExpenseTrakcerHepler;
global using Infrastructure.Email;

global using Serilog.Core;
global using Serilog.Events;

global using Domain.Interfaces;
global using FirebaseAdmin;
global using Google.Apis.Auth.OAuth2;
global using Infrastructure.Data;
global using Infrastructure.Email.Config;
global using Infrastructure.Repositories;
global using Microsoft.AspNetCore.Authentication.JwtBearer;
global using Microsoft.AspNetCore.DataProtection;
global using Microsoft.EntityFrameworkCore;
global using Services.BackgroundJobs;
global using Services.Management;
global using Services.Management.AuthService;
global using Microsoft.OpenApi.Models;
global using AppExpenseTrackerApi.Middlewares;