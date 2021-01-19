﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using Microsoft.Toolkit.Uwp.UI.Lottie.Animatables;
using static Microsoft.Toolkit.Uwp.UI.Lottie.IR.Exceptions;

namespace Microsoft.Toolkit.Uwp.UI.Lottie.IR.RenderingContexts
{
    abstract class PositionRenderingContext : RenderingContext
    {
        PositionRenderingContext()
        {
        }

        public static PositionRenderingContext Create(IAnimatableVector2 position)
            => position.IsAnimated ? new Animated(position) : new Static(position.InitialValue);

        public static RenderingContext WithoutRedundants(RenderingContext context)
        {
            return Compose(Without(context));

            static IEnumerable<RenderingContext> Without(RenderingContext context)
            {
                var currentPosition = Vector2.Zero;

                foreach (var item in context)
                {
                    switch (item)
                    {
                        case Static staticPosition:
                            currentPosition += staticPosition.Position;
                            break;

                        default:
                            if (currentPosition != Vector2.Zero)
                            {
                                yield return new Static(currentPosition);
                                currentPosition = Vector2.Zero;
                            }

                            yield return item;
                            break;
                    }
                }

                if (currentPosition != Vector2.Zero)
                {
                    yield return new Static(currentPosition);
                }
            }
        }

        public static RenderingContext GroupTogether(RenderingContext context)
        {
            return Compose(Group(context));

            static IEnumerable<RenderingContext> Group(RenderingContext items)
            {
                var accumulator = new List<RenderingContext>(items.SubContextCount);
                var positionsAcccumulator = new List<PositionRenderingContext>();

                foreach (var item in items)
                {
                    switch (item)
                    {
                        case PositionRenderingContext position:
                            positionsAcccumulator.Add(position);
                            break;

                        // The position cannot be moved above these types.
                        case AnchorRenderingContext _:
                        case SizeRenderingContext _:
                            foreach (var i in positionsAcccumulator)
                            {
                                yield return i;
                            }

                            foreach (var i in accumulator)
                            {
                                yield return i;
                            }

                            yield return item;
                            positionsAcccumulator.Clear();
                            accumulator.Clear();
                            break;

                        default:
                            accumulator.Add(item);
                            break;
                    }
                }

                foreach (var item in positionsAcccumulator)
                {
                    yield return item;
                }

                foreach (var item in accumulator)
                {
                    yield return item;
                }
            }
        }

        public sealed class Animated : PositionRenderingContext
        {
            internal Animated(IAnimatableVector2 position)
                => Position = position;

            public IAnimatableVector2 Position { get; }

            public override RenderingContext WithOffset(Vector2 offset)
            {
                if (offset.X == 0 && offset.Y == 0)
                {
                    return this;
                }

                return new Animated(Position.Type switch
                {
                    AnimatableVector2Type.Vector2 => ((AnimatableVector2)Position).WithOffset(offset),
                    AnimatableVector2Type.XY => ((AnimatableXY)Position).WithOffset(offset),
                    _ => throw Unreachable,
                });
            }

            public override RenderingContext WithTimeOffset(double timeOffset)
                => new Animated(Position.WithTimeOffset(timeOffset));

            public override bool IsAnimated => true;

            public override string ToString() => $"Animated Position {Position}";
        }

        public sealed class Static : PositionRenderingContext
        {
            internal Static(Vector2 position)
                => Position = position;

            public Vector2 Position { get; }

            public override bool IsAnimated => false;

            public override RenderingContext WithOffset(Vector2 offset)
                => offset.X == 0 && offset.Y == 0
                    ? this
                    : new Static(Position + offset);

            public override RenderingContext WithTimeOffset(double timeOffset) => this;

            public override string ToString() => $"Static Position {Position}";
        }
    }
}
