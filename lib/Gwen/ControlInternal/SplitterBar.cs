﻿using System;
using Gwen.Controls;

namespace Gwen.ControlInternal
{
    /// <summary>
    /// Splitter bar.
    /// </summary>
    public class SplitterBar : Dragger
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="SplitterBar"/> class.
        /// </summary>
        /// <param name="parent">Parent control.</param>
        public SplitterBar(Controls.ControlBase parent)
            : base(parent)
        {
            Target = this;
            RestrictToParent = true;
        }

        /// <summary>
        /// Renders the control using specified skin.
        /// </summary>
        /// <param name="skin">Skin to use.</param>
        protected override void Render(Skin.SkinBase skin)
        {
            if (ShouldDrawBackground)
                skin.DrawButton(this, true, false, IsDisabled);
        }

        /// <summary>
        /// Lays out the control's interior according to alignment, padding, dock etc.
        /// </summary>
        /// <param name="skin">Skin to use.</param>
        protected override void Layout(Skin.SkinBase skin)
        {
            MoveTo(X, Y);
        }
    }
}