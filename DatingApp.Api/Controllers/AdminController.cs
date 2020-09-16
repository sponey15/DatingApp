using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using CloudinaryDotNet;
using CloudinaryDotNet.Actions;
using DatingApp.Api.Data;
using DatingApp.Api.Dtos;
using DatingApp.Api.Helpers;
using DatingApp.Api.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace DatingApp.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AdminController : ControllerBase
    {
        private readonly DataContext _context;
        private readonly UserManager<User> _userManager;
        private readonly IDatingRepository _repo;
        private readonly IMapper _mapper;
        private readonly IOptions<CloudinarySettings> _cloudinaryConfig;
        private Cloudinary _cloudinary;
        public AdminController(DataContext context, UserManager<User> userManager, IDatingRepository repo,
                               IMapper mapper, IOptions<CloudinarySettings> cloudinaryConfig)
        {
            _cloudinaryConfig = cloudinaryConfig;
            _mapper = mapper;
            _repo = repo;
            _userManager = userManager;
            _context = context;

            Account acc = new Account(
                _cloudinaryConfig.Value.CloudName,
                _cloudinaryConfig.Value.ApiKey,
                _cloudinaryConfig.Value.ApiSecret
            );

            _cloudinary = new Cloudinary(acc);
        }

    [Authorize(Policy = "RequireAdminRole")]
    [HttpGet("usersWithRoles")]
    public async Task<IActionResult> GetUsersWithRoles()
    {
        var userList = await _context.Users
            .OrderBy(x => x.UserName)
            .Select(user => new
            {
                Id = user.Id,
                UserName = user.UserName,
                Roles = (from userRole in user.UserRoles
                         join role in _context.Roles
                         on userRole.RoleId
                         equals role.Id
                         select role.Name).ToList()
            }).ToListAsync();

        return Ok(userList);
    }

    [Authorize(Policy = "RequireAdminRole")]
    [HttpPost("editRoles/{userName}")]
    public async Task<IActionResult> EditRoles(string userName, RoleEditDto roleEditDto)
    {
        var user = await _userManager.FindByNameAsync(userName);

        var userRoles = await _userManager.GetRolesAsync(user);

        var selectedRoles = roleEditDto.RoleNames;

        // selected = selectedRoles != null ? selectedRoles : new string[] {};
        selectedRoles = selectedRoles ?? new string[] { };

        var result = await _userManager.AddToRolesAsync(user, selectedRoles.Except(userRoles));

        if (!result.Succeeded)
            return BadRequest("Failed to add to roles");

        result = await _userManager.RemoveFromRolesAsync(user, userRoles.Except(selectedRoles));

        if (!result.Succeeded)
            return BadRequest("Failed to remove the roles");

        return Ok(await _userManager.GetRolesAsync(user));
    }

    [Authorize(Policy = "ModeratePhotoRole")]
    [HttpGet("photosForModeration")]
    public async Task<IActionResult> GetPhotosForModeration()
    {
        // var photoList = await _context.Photos
        //     .OrderBy(x => x.DateAdded)
        //     .Where(p => p.IsApproved == false)
        //     .Select(photo => new
        //     {
        //         Id = photo.Id,
        //         UserName = photo.Url, // tutaj wystarczylo dac UserName = photo.User.UserName
        //         Description = photo.Description,
        //         DateAdded = photo.DateAdded,
        //         IsMain = photo.IsMain,
        //         PublicId = photo.PublicID,
        //         IsApproved = photo.IsApproved
        //     }).ToListAsync();
        // i wtedy poprostu return Ok(photoList);

        var photoList = await _context.Photos
            .OrderBy(x => x.DateAdded)
            .IgnoreQueryFilters()
            .Where(p => p.IsApproved == false)
            .ToListAsync();

        var photoListForReturn = _mapper.Map<IEnumerable<PhotoForReturnDto>>(photoList);

        return Ok(photoListForReturn);
    }

    [Authorize(Policy = "ModeratePhotoRole")]
    [HttpPost("acceptPhoto/{photoId}")]
    public async Task<IActionResult> AcceptPhoto(int photoId)
    {
        var photo = await _repo.GetPhoto(photoId);

        photo.IsApproved = true;

        await _repo.SaveAll();

        return NoContent();
    }

    [Authorize(Policy = "ModeratePhotoRole")]
    [HttpDelete("rejectPhoto/{photoId}")]
    public async Task<IActionResult> RejectPhoto(int photoId)
    {
        var photoFromRepo = await _repo.GetPhoto(photoId);

        if (photoFromRepo.PublicID != null) //z cloudinary zdjecie
        {
            var deleteParams = new DeletionParams(photoFromRepo.PublicID);

            var result = _cloudinary.Destroy(deleteParams);

            if (result.Result == "ok")
            {
                _repo.Delete(photoFromRepo);
            }
        }

        if (photoFromRepo.PublicID == null) //nie z cloudinary (z tej random stronki dlatego nie maja PublicID)
        {
            _repo.Delete(photoFromRepo);
        }

        if (await _repo.SaveAll())
            return Ok();

        return BadRequest("Failed to delete the photo");
    }
}
}