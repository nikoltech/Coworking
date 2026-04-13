using System;
using System.Collections.Generic;
using System.Text;

namespace Coworking.Infrastructure.Synchronization;

// Структура для ключа (ноль аллокаций)
internal readonly record struct RangeKey(Guid DeskId, DateTimeOffset Start, DateTimeOffset End);
