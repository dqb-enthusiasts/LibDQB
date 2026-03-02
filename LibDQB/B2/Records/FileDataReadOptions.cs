using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LibDQB.B2.Records;

public sealed record FileDataReadOptions
{
	public FileShare FileShare { get; init; } = FileShare.None;
}
