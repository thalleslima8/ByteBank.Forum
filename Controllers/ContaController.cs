using ByteBank.Forum.Models;
using ByteBank.Forum.ViewModels;
using Microsoft.AspNet.Identity;
using Microsoft.AspNet.Identity.Owin;
using Microsoft.Owin.Security;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;

namespace ByteBank.Forum.Controllers
{
    public class ContaController : Controller
    {
        private UserManager<UsuarioAplicacao> _userManager;
        public UserManager<UsuarioAplicacao> UserManager
        {
            get
            {
                if (_userManager == null)
                {
                    var contextOwin = HttpContext.GetOwinContext();
                    _userManager = contextOwin.GetUserManager<UserManager<UsuarioAplicacao>>();
                }
                return _userManager;
            }
            set
            {
                _userManager = value;
            }
        }

        private SignInManager<UsuarioAplicacao, string> _signInManager;
        public SignInManager<UsuarioAplicacao, string> SignInManager
        {
            get
            {
                if (_signInManager == null)
                {
                    var contextOwin = HttpContext.GetOwinContext();
                    _signInManager = contextOwin.GetUserManager<SignInManager<UsuarioAplicacao, string>>();
                }
                return _signInManager;
            }
            set
            {
                _signInManager = value;
            }
        }

        public IAuthenticationManager AuthenticationManager
        {
            get
            {
                var contextoOwin = Request.GetOwinContext();
                return contextoOwin.Authentication;
            }
        }

        // GET: Conta
        public ActionResult Registrar()
        {
            return View();
        }

        [HttpPost]
        public async Task<ActionResult> Registrar(ContaRegistrarViewModel modelo)
        {
            if (ModelState.IsValid)
            {
               
                var novoUsuario = new UsuarioAplicacao();

                novoUsuario.Email = modelo.Email;
                novoUsuario.UserName = modelo.UserName;
                novoUsuario.Nome = modelo.Nome;

                var usuario = await UserManager.FindByEmailAsync(modelo.Email);

                if(usuario != null)
                    return View("AguardandoConfirmacao");


                var resultado = await UserManager.CreateAsync(novoUsuario, modelo.Senha);

                if (resultado.Succeeded)
                {
                    //Enviar o email confirmacao
                    await EnviarEmailConfirmacaoAsync(novoUsuario);

                    return View("AguardandoConfirmacao");
                }

                AdicionaErros(resultado);

            }
            return View();
        }

        //GET: Login
        public async Task<ActionResult> Login()
        {
            return View();
        }

        [HttpPost]
        public async Task<ActionResult> Login(ContaLoginViewModel modelo)
        {
            if (ModelState.IsValid)
            {
                var usuario = await UserManager.FindByEmailAsync(modelo.Email);

                if (usuario == null)
                    return SenhaOuUsuarioInvalidos("Credenciais inválidas!");

                if(!usuario.EmailConfirmed)
                    return View("AguardandoConfirmacao");

                var resultadoSignIn = await SignInManager.PasswordSignInAsync(
                                                        usuario.UserName,
                                                        modelo.Senha,
                                                        isPersistent: modelo.ContinuarLogado,
                                                        shouldLockout: true);
                switch (resultadoSignIn)
                {
                    case SignInStatus.Success:
                        return RedirectToAction("Index", "Home");
                    case SignInStatus.LockedOut:
                        var senhaCorreta = await UserManager.CheckPasswordAsync(usuario, modelo.Senha);

                        if (senhaCorreta)
                        {
                            ModelState.AddModelError("", "Você errou a senha mais de 3 vezes!");
                            ModelState.AddModelError("", "A conta está bloqueada! Aguarde 5 minutos e tente novamente.");
                        }
                        else
                        {
                            return SenhaOuUsuarioInvalidos("Credenciais inválidas!");
                        }
                        break;
                            
                    default:
                        return SenhaOuUsuarioInvalidos("Credenciais inválidas!");
                }

            }

            return View(modelo);
        }

        //GET
        public ActionResult EsqueciSenha()
        {
            return View();
        }

        [HttpPost]
        public async Task<ActionResult> EsqueciSenha(ContaEsqueciSenhaViewModel modelo)
        {
            if (ModelState.IsValid)
            {
                var usuario = await UserManager.FindByEmailAsync(modelo.Email);
                if(usuario != null)
                {
                    //Gerar token de reset da senha
                    var token = await UserManager.GeneratePasswordResetTokenAsync(usuario.Id);

                    //Gerar Url para usuario
                    var linkCallBack = Url.Action(
                                            "ConfirmacaoAlterouSenha",
                                            "Conta",
                                            new { usuarioId = usuario.Id, token = token },
                                            Request.Url.Scheme);

                    //Enviar e-mail
                    await UserManager.SendEmailAsync(
                        usuario.Id,
                        "Fórum ByteBank - Alteração de Senha",
                        $"Bem Vindo ao fórum ByteBank, clique aqui {linkCallBack} para alterar a sua senha!"
                    );

                }
                return View("EmailAlteracaoSenhaEnviado");
            }
            return View();
        }

        //GET: Confirmação de Senha
        public ActionResult ConfirmacaoAlterouSenha(string usuarioId, string token)
        {
            var modelo = new ContaConfirmacaoAlterouSenhaViewModel()
            {
                UsuarioId = usuarioId,
                Token = token
            };
            return View();
        }

        [HttpPost]
        public async Task<ActionResult> ConfirmacaoAlterouSenha(ContaConfirmacaoAlterouSenhaViewModel modelo)
        {
            if (ModelState.IsValid)
            {
                //Verifica Token
                var resultadoNovaSenha = await UserManager.ResetPasswordAsync(modelo.UsuarioId, modelo.Token, modelo.NovaSenha);

                if (resultadoNovaSenha.Succeeded)
                {
                    return RedirectToAction("Index", "Home");
                }
                AdicionaErros(resultadoNovaSenha);
            }
            return View();
        }

        private ActionResult SenhaOuUsuarioInvalidos(string msg)
        {
            ModelState.AddModelError("", msg);
            return View("Login");
        }

        public async Task<ActionResult> ConfirmacaoEmail(string usuarioId, string token)
        {
            if (usuarioId == null || token == null)
                return View("Error");

            var resultado = await UserManager.ConfirmEmailAsync(usuarioId, token);

            if (resultado.Succeeded)
                return RedirectToAction("Index", "Home");

            return View("Error");
        }

        private async Task EnviarEmailConfirmacaoAsync(UsuarioAplicacao usuario)
        {
            var token = await UserManager.GenerateEmailConfirmationTokenAsync(usuario.Id);
            var linkCallBack =
                Url.Action(
                    "ConfirmacaoEmail",
                    "Conta",
                    new { usuarioId = usuario.Id, token = token },
                    Request.Url.Scheme);

            await UserManager.SendEmailAsync(
                usuario.Id,
                "Fórum ByteBank - Confirmação de E-mail",
                //$"Bem Vindo ao fórum ByteBank, use o código {token} para confirmar seu endereço de e-mail."
                $"Bem Vindo ao fórum ByteBank, clique aqui {linkCallBack} para confirmar seu endereço de e-mail!"
            );
        }

        [HttpPost]
        public ActionResult Logoff()
        {
            AuthenticationManager.SignOut(DefaultAuthenticationTypes.ApplicationCookie);
            return RedirectToAction("Index", "Home");
        }

        private void AdicionaErros(IdentityResult resultado)
        {
            foreach (var erro in resultado.Errors)
            {
                ModelState.AddModelError("", erro);
            }
        }
    }
}