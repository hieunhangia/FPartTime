using System;
using System.Collections.Generic;
using System.Text;
using Repository.Constants;
using Repository.Models.Users;
using Riok.Mapperly.Abstractions;

namespace Service.ApplicationServices.Employer
{
    public class EmployerProfileService
    {


    }


    public class EmployerProfileResponseDto
    {
        public string? LogoFilePath { get; set; }
        public string? Description { get; set; }
        public string? ContactPhone { get; set; }
    }


    [Mapper(RequiredMappingStrategy = RequiredMappingStrategy.None)]
    public static partial class EmployerProfileMapper
    {
        public static partial EmployerProfileResponseDto MapToDto(this EmployerProfile profile);

        public static partial void UpdateEntity(this EmployerProfileResponseDto dto, EmployerProfile profile);
    }
}
