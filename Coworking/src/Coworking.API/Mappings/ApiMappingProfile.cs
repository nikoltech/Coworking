using AutoMapper;
using Coworking.API.Models.Requests;
using Coworking.API.Models.Responces;
using Coworking.Application.Features.Bookings.Commands.Create;
using Coworking.Application.Features.Bookings.Commands.Create.Requests;
using Coworking.Application.Features.Bookings.Queries.GetDeskAvailability;
using Coworking.Application.Features.Bookings.Queries.GetDeskAvailability.Dtos;

namespace Coworking.API.Mappings;

public class ApiMappingProfile : Profile
{
    public ApiMappingProfile()
    {
        CreateMap<BookingMetadataRequest, BookingMetadata?>();
        CreateMap<CreateBookingRequest, CreateBookingCommand>();

        CreateMap<GetDeskAvailabilityRequest, GetDeskAvailabilityQuery>()
            .ForCtorParam("dateFrom", opt => opt.MapFrom(src => src.DateFrom))
            .ForCtorParam("dateTo", opt => opt.MapFrom(src => src.DateTo));

        CreateMap<TimeSlotDto, TimeSlotResponse>();
        CreateMap<Application.Features.Bookings.Queries.GetDeskAvailability.Responses.DeskAvailabilityResponse, DeskAvailabilityResponse>();
    }
}