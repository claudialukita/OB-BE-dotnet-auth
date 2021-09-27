using AutoMapper;
using IdentityModel.Client;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;
using API.Auth.DTO;
using Security.DTO;
using BLL;
using DAL.Model;

namespace Auth
{

    [ApiController]
    [Route("[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly IUserAuthorizationService _authService;
        private readonly ILogger _logger;
        private readonly IMapper _mapper;

        public AuthController(ILogger<AuthController> logger, IUserAuthorizationService authService)
        {
            _authService = authService;
            _logger = logger;

            MapperConfiguration configMapper = new MapperConfiguration(m =>
            {
                m.CreateMap<TokenResponse, UserTokenDTO>();
                m.CreateMap<User, UserInfoTokenDTO>();
            });

            _mapper = configMapper.CreateMapper();
        }

        /// <summary>
        /// Login
        /// </summary>
        /// <param name="user">Model of user login object.</param>
        /// <response code="200">Request successful.</response>
        /// <response code="400">Request failed because of an exception.</response>
        [ProducesResponseType(typeof(UserInfoTokenDTO), 200)]
        [HttpPost]
        [Route("login")]
        public async Task<IActionResult> Get([FromBody] UserLoginDTO userLoginDto)
        {
            try
            {
                TokenResponse userToken = await _authService.LoginAsync(userLoginDto.UserName, userLoginDto.Password);

                User user = await _authService.GetUserAsync(userLoginDto.UserName);

                UserInfoTokenDTO userInfoDto = _mapper.Map<User, UserInfoTokenDTO>(user);

                userInfoDto.TokenResponse = _mapper.Map<TokenResponse, UserTokenDTO>(userToken);

                return new OkObjectResult(userInfoDto);
            }
            catch (Exception e)
            {
                return new BadRequestObjectResult(e);
            }

        }

        /// <summary>
        /// Check authorization only
        /// </summary>
        /// <response code="200">Request successful.</response>
        /// <response code="400">Request failed because of an exception.</response>
        [ProducesResponseType(200)]
        [HttpGet]
        [Route("check")]
        [Authorize]
        public ActionResult CheckAuthRole()
        {
            return Ok();
        }

        /// <summary>
        /// Check authorization with role
        /// </summary>
        /// <response code="200">Request successful.</response>
        /// <response code="400">Request failed because of an exception.</response>
        [ProducesResponseType(200)]
        [HttpGet]
        [Route("check-with-role")]
        [AuthorizedByRole("Admin")]
        public ActionResult CheckAuthOnly()
        {
            return Ok();
        }

    }

}