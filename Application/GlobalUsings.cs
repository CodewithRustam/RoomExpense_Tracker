global using System.ComponentModel.DataAnnotations;
global using System.Security.Claims;
global using System.Globalization;

global using Microsoft.AspNetCore.Mvc.Rendering;
global using Microsoft.Extensions.Caching.Memory;
global using Microsoft.Extensions.DependencyInjection;
global using Microsoft.Extensions.Configuration;
global using Microsoft.EntityFrameworkCore;
global using Microsoft.AspNetCore.Identity;
global using Microsoft.AspNetCore.Http;
global using Microsoft.Extensions.Hosting;
global using Microsoft.Extensions.Logging;

global using QuestPDF.Fluent;
global using QuestPDF.Helpers;
global using QuestPDF.Infrastructure;

global using Domain.AppUser;
global using Domain.Entities;
global using Domain.Interfaces;
global using Domain.Entities.Dto;
global using Domain.Exceptions;

global using Services.ViewModels.ApiViewModels;
global using Services.ViewModels.ApiReponse;
global using Services.Interfaces;
global using Services.ViewModels;

global using ExpenseTrakcerHepler;
global using FirebaseAdmin.Messaging;

global using Infrastructure.Data;
global using Infrastructure.Email;