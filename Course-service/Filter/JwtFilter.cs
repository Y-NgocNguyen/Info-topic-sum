using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace Course_service.Filter
{
    public class JwtFilter : IActionFilter
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<JwtFilter> _logger;
        private string _roles;

        public JwtFilter(IConfiguration configuration, ILogger<JwtFilter> logger, string roles)
        {
            _configuration = configuration;
            _logger = logger;
            _roles = roles;
        }

        public void OnActionExecuted(ActionExecutedContext context)
        {
        }

        public void OnActionExecuting(ActionExecutingContext context)
        {
            var token = context.HttpContext.Request.Headers["Authorization"].FirstOrDefault()?.Split(" ").Last();
            if (token != null)
            {
                try
                {
                    var tokenHandler = new JwtSecurityTokenHandler();
                    var key = Encoding.ASCII.GetBytes(_configuration["JWT:Secret"]);

                    var tokenValidationParameters = new TokenValidationParameters
                    {
                        ValidateIssuerSigningKey = true,
                        IssuerSigningKey = new SymmetricSecurityKey(key),

                        ValidateLifetime = true,

                        ValidateIssuer = false,
                        ValidateAudience = false
                    };

                    var claimsPrincipal = tokenHandler.ValidateToken(token, tokenValidationParameters, out var validatedToken);

                    context.HttpContext.User = claimsPrincipal;
                    string[] roleArray = _roles.Split(',').Select(r => r.Trim().ToLower()).ToArray();

                    bool validateRoles = claimsPrincipal.Claims
                         .Where(c => c.Type == ClaimTypes.Role)
                         .Select(c => c.Value)
                         .Any(c => roleArray.Contains(c.ToLower()));
                    ;

                    if (!validateRoles)
                    {
                        context.Result = new BadRequestObjectResult("you not permison");
                        return;
                    }

                    _logger.LogInformation("------------------------");
                    foreach (var claim in claimsPrincipal.Claims)
                    {
                        _logger.LogInformation($" {claim.Type} : {claim.Value}");
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogInformation(ex.Message);
                    context.Result = new UnauthorizedResult();
                    return;
                }
            }
            else
            {
                context.Result = new UnauthorizedResult();
            }
        }
    }
}