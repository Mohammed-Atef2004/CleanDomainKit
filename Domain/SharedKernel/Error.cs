using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Domain.SharedKernel
{
    public record Error(string Code, string Message)
    {
        public static readonly Error None = new("", "");
    }

}
