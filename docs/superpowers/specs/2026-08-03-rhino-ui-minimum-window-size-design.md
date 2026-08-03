# Rhino UI Minimum Window Size Design

## Goal

Prevent the AIQ Urban Rhino UI from being resized to dimensions that make the interface unusable, while preserving its existing default size and allowing unrestricted enlargement.

## Design

`UrbanHtmlUi` will retain its current 520-by-900-pixel default window dimensions. Two named constants will define the minimum window dimensions as 312 pixels wide and 630 pixels high, corresponding to 60% of the default width and 70% of the default height at the time of this change.

The Eto `Form` created by `UrbanHtmlUi.OpenWindow` will receive these dimensions through its `MinimumSize` property. The constraint belongs on the top-level window only; the embedded `WebView` will continue to follow the existing layout and will not duplicate the minimum-size constraint. No maximum size will be set.

## Testing

A focused NUnit test in the existing Rhino plugin test project will open the window under the Rhino test fixture and verify both behaviors:

- the initial window size remains 520 by 900 pixels;
- the minimum window size is 312 by 630 pixels.

The test will close the window during cleanup so the static window reference and UI resources do not leak into other tests.

## Scope

This change affects only the Rhino plugin's Eto window construction and its test coverage. It does not change the Vue application, server lifecycle, default window dimensions, or resizing behavior above the minimum dimensions.
