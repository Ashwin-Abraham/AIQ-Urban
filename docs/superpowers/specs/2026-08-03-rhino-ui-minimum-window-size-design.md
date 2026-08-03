# Rhino UI Minimum Window Size Design

## Goal

Prevent the AIQ Urban Rhino UI from being resized to dimensions that make the interface unusable, while preserving its existing default size and allowing unrestricted enlargement.

## Design

`UrbanHtmlUi` will retain its current 520-by-900-pixel default window dimensions. Two named constants will define the minimum window dimensions as 312 pixels wide and 630 pixels high, corresponding to 60% of the default width and 70% of the default height at the time of this change.

The Eto `Form` created by `UrbanHtmlUi.OpenWindow` will receive these dimensions through its `MinimumSize` property. The constraint belongs on the top-level window only; the embedded `WebView` will continue to follow the existing layout and will not duplicate the minimum-size constraint. No maximum size will be set.

## Verification

No automated test will be added for this small Eto property assignment. The Rhino plugin project will be built to verify that the `MinimumSize` API and the new constants compile, and the final diff will be inspected to confirm that the default size remains 520 by 900 pixels, the minimum is 312 by 630 pixels, and no maximum is introduced.

## Scope

This change affects only the Rhino plugin's Eto window construction and its design documentation. It does not change the Vue application, server lifecycle, default window dimensions, or resizing behavior above the minimum dimensions.
