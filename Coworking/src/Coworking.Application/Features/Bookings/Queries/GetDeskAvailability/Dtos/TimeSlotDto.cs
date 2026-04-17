using MediatR;
using System;
using System.Collections.Generic;
using System.Text;

namespace Coworking.Application.Features.Bookings.Queries.GetDeskAvailability.Dtos;

public record TimeSlotDto(DateTimeOffset Start, DateTimeOffset End, bool IsAvailable);
