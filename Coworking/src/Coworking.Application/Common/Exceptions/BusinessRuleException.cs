using System;
using System.Collections.Generic;
using System.Text;

namespace Coworking.Application.Common.Exceptions;

public sealed class BusinessRuleException(string message) : Exception(message);
