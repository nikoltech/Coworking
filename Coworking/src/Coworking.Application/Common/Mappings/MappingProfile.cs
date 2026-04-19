using AutoMapper;
using Coworking.Application.Features.Coworkings.Queries.GetCoworkings.Dtos;
using Coworking.Application.Features.Desks.Queries.GetDesks.Dtos;
using Coworking.Domain.Entities;

namespace Coworking.Application.Common.Mappings;

public class MappingProfile : Profile
{
    public MappingProfile()
    {
        CreateMap<Domain.Entities.Coworking, CoworkingDto>()
            .ForMember(dest => dest.TimeZone, opt => opt.MapFrom(src => src.TimeZoneId));

        CreateMap<Desk, DeskDto>();
    }
}