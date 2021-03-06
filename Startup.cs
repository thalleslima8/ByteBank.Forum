﻿using ByteBank.Forum.Models;
using Microsoft.AspNet.Identity;
using Microsoft.AspNet.Identity.Owin;
using Microsoft.AspNet.Identity.EntityFramework;
using Microsoft.Owin;
using Owin;
using System.Data.Entity;
using ByteBank.Forum.App_Start.Identity;
using Microsoft.Owin.Security.Cookies;
using System;

[assembly: OwinStartup(typeof(ByteBank.Forum.Startup))]

namespace ByteBank.Forum
{
    public class Startup
    {
        public void Configuration(IAppBuilder builder)
        {
            builder.CreatePerOwinContext<DbContext>(() =>
                new IdentityDbContext<UsuarioAplicacao>("DefaultConnection"));

            builder.CreatePerOwinContext<IUserStore<UsuarioAplicacao>>(
                (opcoes, contextoOwion) =>
                {
                    var dbContext = contextoOwion.Get<DbContext>();
                    return new UserStore<UsuarioAplicacao>(dbContext);
                });

            builder.CreatePerOwinContext<UserManager<UsuarioAplicacao>>(
                (opcoes, contextoOwion) =>
                {
                    var userStore = contextoOwion.Get<IUserStore<UsuarioAplicacao>>();
                    var userManager = new UserManager<UsuarioAplicacao>(userStore);

                    var userValidator = new UserValidator<UsuarioAplicacao>(userManager);

                    userValidator.RequireUniqueEmail = true;

                    userManager.UserValidator = userValidator;
                    userManager.PasswordValidator = new SenhaValidador()
                    {
                        TamanhoRequerido = 6,
                        ObrigatorioCaracteresLowerCase = true,
                        ObrigatorioCaracteresUpperCase = true,
                        ObrigatorioDigitos = true
                    };

                    userManager.EmailService = new EmailService();

                    var dataProvider = opcoes.DataProtectionProvider;
                    var dataProtectioProviderCreated = dataProvider.Create("ByteBank.Forum");

                    userManager.UserTokenProvider = new DataProtectorTokenProvider<UsuarioAplicacao>(dataProtectioProviderCreated);

                    userManager.MaxFailedAccessAttemptsBeforeLockout = 3;
                    userManager.DefaultAccountLockoutTimeSpan = TimeSpan.FromMinutes(5);
                    userManager.UserLockoutEnabledByDefault = true;

                    return userManager;
                });

            builder.CreatePerOwinContext<SignInManager<UsuarioAplicacao, string>>(
               (opcoes, contextoOwion) =>
               {
                   var userManager = contextoOwion.Get<UserManager<UsuarioAplicacao>>();
                   var singInManager = new SignInManager<UsuarioAplicacao, string>(
                                               userManager,
                                               contextoOwion.Authentication);

                   return singInManager;
               });

            builder.UseCookieAuthentication(new CookieAuthenticationOptions { 
                AuthenticationType = DefaultAuthenticationTypes.ApplicationCookie
            });
        }
    }
}