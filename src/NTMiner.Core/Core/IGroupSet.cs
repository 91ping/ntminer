﻿using System;
using System.Collections.Generic;

namespace NTMiner.Core {
    public interface IGroupSet : IEnumerable<IGroup> {
        int Count { get; }
        bool Contains(Guid groupId);
        bool TryGetGroup(Guid groupId, out IGroup group);
    }
}
