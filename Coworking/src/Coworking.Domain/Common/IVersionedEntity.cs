using System;
using System.Collections.Generic;
using System.Text;

namespace Coworking.Domain.Common;

public interface IVersionedEntity
{
    Guid Version { get; set; }
}
