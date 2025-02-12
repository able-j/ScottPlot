﻿using ScottPlot.Axis;

namespace ScottPlot.Layouts;

public class StandardLayoutMaker : ILayoutMaker
{
    public Layout GetLayout(PixelSize figureSize, IEnumerable<IPanel> panels)
    {
        // Regenerate ticks using the figure area (not the data area)
        // to create a first-pass estimate of the space needed for axis panels.
        // Ticks require recalculation once more after the axes are repositioned
        // according to the layout determined by this function.

        panels.OfType<IXAxis>()
            .ToList()
            .ForEach(xAxis => xAxis.TickGenerator.Regenerate(xAxis.Range, xAxis.Edge, figureSize.Width));

        panels.OfType<IYAxis>()
            .ToList()
            .ForEach(yAxis => yAxis.TickGenerator.Regenerate(yAxis.Range, yAxis.Edge, figureSize.Height));

        Layout layout = MakeFinalLayout(figureSize, panels);

        return layout;
    }

    /// <summary>
    /// Given panels all of a single edge, update the panelOffset dictionary with their offsets
    /// </summary>
    private void CalculateOffsets(IEnumerable<IPanel> panels, Dictionary<IPanel, float> sizes, Dictionary<IPanel, float> offsets)
    {
        float offset = 0;
        foreach (IPanel panel in panels)
        {
            offsets[panel] = offset;
            offset += sizes[panel];
        }
    }

    private Layout MakeFinalLayout(PixelSize figureSize, IEnumerable<IPanel> panels)
    {
        // Plot edges with visible axes require padding between the figure rectangle and data rectangle.
        // Panels have size and increase the amount of padding needed.
        // This function iterates all panels and records the total padding needed and offset of each panel.

        IEnumerable<IPanel> leftPanels = panels.Where(x => x.Edge == Edge.Left);
        IEnumerable<IPanel> rightPanels = panels.Where(x => x.Edge == Edge.Right);
        IEnumerable<IPanel> bottomPanels = panels.Where(x => x.Edge == Edge.Bottom);
        IEnumerable<IPanel> topPanels = panels.Where(x => x.Edge == Edge.Top);

        // Measure the size of every panel
        Dictionary<IPanel, float> panelSizes = panels.ToDictionary(x => x, y => y.Measure());

        // Determine the offset needed for each panel
        Dictionary<IPanel, float> panelOffsets = new();
        CalculateOffsets(leftPanels, panelSizes, panelOffsets);
        CalculateOffsets(rightPanels, panelSizes, panelOffsets);
        CalculateOffsets(bottomPanels, panelSizes, panelOffsets);
        CalculateOffsets(topPanels, panelSizes, panelOffsets);

        // Determine how large the data area should be
        PixelPadding paddingNeededForPanels = new(
            left: leftPanels.Select(x => panelSizes[x]).Sum(),
            right: rightPanels.Select(x => panelSizes[x]).Sum(),
            bottom: bottomPanels.Select(x => panelSizes[x]).Sum(),
            top: topPanels.Select(x => panelSizes[x]).Sum());

        PixelRect dataRect = new(
            left: paddingNeededForPanels.Left,
            right: figureSize.Width - paddingNeededForPanels.Right,
            bottom: figureSize.Height - paddingNeededForPanels.Bottom,
            top: paddingNeededForPanels.Top);

        return new Layout(figureSize, dataRect, panelSizes, panelOffsets);
    }
}
