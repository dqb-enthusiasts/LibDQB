using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LibDQB.DQB2Minimap;

public interface IMinimap : IReadOnlyMinimap, IGrid<MinimapTile> { }
