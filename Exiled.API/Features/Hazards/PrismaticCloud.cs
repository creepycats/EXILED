// -----------------------------------------------------------------------
// <copyright file="PrismaticCloud.cs" company="Exiled Team">
// Copyright (c) Exiled Team. All rights reserved.
// Licensed under the CC BY-SA 3.0 license.
// </copyright>
// -----------------------------------------------------------------------

namespace Exiled.API.Features.Hazards
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    using RelativePositioning;

    using PrismaticCloudHazard = global::Hazards.PrismaticCloud;

    /// <summary>
    /// A wrapper for <see cref="PrismaticCloudHazard"/>.
    /// </summary>
    public class PrismaticCloud : TemporaryHazard
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="PrismaticCloud"/> class.
        /// </summary>
        /// <param name="hazard">The <see cref="PrismaticCloudHazard"/> instance.</param>
        public PrismaticCloud(PrismaticCloudHazard hazard)
            : base(hazard)
        {
            Base = hazard;
        }

        /// <summary>
        /// Gets the <see cref="PrismaticCloudHazard"/>.
        /// </summary>
        public new PrismaticCloudHazard Base { get; }

        /// <summary>
        /// Gets or sets a list of players that will be ignored by the prismatic cloud.
        /// </summary>
        public IReadOnlyCollection<Player> IgnoredPlayers
        {
            get => Base.IgnoredTargets.Select(hub => Player.Get(hub)).ToList();
            set => Base.IgnoredTargets = value.Select(player => player.ReferenceHub).ToList();
        }

        /// <summary>
        /// Gets or sets the synced position.
        /// </summary>
        [Obsolete("Use SynchronizedPosition")]
        public RelativePosition SynchronisedPosition
        {
            get => SynchronizedPosition;
            set => SynchronizedPosition = value;
        }

        /// <summary>
        /// Gets or sets the synced position.
        /// </summary>
        public RelativePosition SynchronizedPosition
        {
            get => Base.SynchronizedPosition;
            set => Base.SynchronizedPosition = value;
        }
    }
}
