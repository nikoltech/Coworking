using AutoMapper;
using Coworking.Application.Features.Desks.Queries.GetDesks.Dtos;
using Coworking.Domain.Entities;

namespace Coworking.Application.Common.Mappings;

public class MappingProfile : Profile
{
    public MappingProfile()
    {
        CreateMap<Desk, DeskDto>();
    }
}