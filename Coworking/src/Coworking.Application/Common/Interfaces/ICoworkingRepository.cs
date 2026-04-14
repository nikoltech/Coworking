using System;
using System.Collections.Generic;
using System.Text;

namespace Coworking.Application.Common.Interfaces;

public interface ICoworkingRepository
{
    Task<Domain.Entities.Coworking> GetByDeskIdAsync(int deskId, CancellationToken ct);
}
