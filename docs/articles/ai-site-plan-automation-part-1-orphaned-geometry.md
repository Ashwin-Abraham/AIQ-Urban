# AI Can't Site Plan Because Your CAD Model Doesn't Know What a Building Is

I believe that we have all the necessary elements to achieve meaningful automation in master planning.

But I do not think the first step is asking AI to magically design a whole masterplan from a prompt.

The first step is more basic:

The software needs to actually understand what is in the plan.

A developer rarely asks:

"Can we draw another block here?"

They ask things like:

1. **How much total built-up area can this site hold?**
2. **What happens to FAR if this parcel changes?**
3. **What happens to coverage if the footprint grows?**
4. **What is the use mix if the lower floors become retail?**
5. **What happens if we preserve more open space or widen a road?**

In a normal CAD workflow, the model does not really know the answer to these questions.

The building may look like a building. It may be placed neatly on a "Building" layer. But most of the time it is still just geometry.

This is the core problem I am exploring with AIQ.

AI cannot site plan properly if the thing it is looking at is not really a site plan.

It is just orphaned geometry.

## The Problem With Orphaned Geometry

Geometry is obviously important. Architects think through geometry. We draw, model, test, present and build with geometry.

So the problem is not geometry.

The problem is geometry that has been disconnected from meaning.

A rectangle might mean a building.

A line might mean a road.

A closed curve might mean a parcel.

A hatch might mean open space.

But the software often does not truly know that. It only knows that there is a curve, a surface, a hatch or a solid sitting somewhere in the file.

Layers help a lot with organisation. This is not an attack on CAD or Rhino. These tools are excellent for modelling, drawing and visualising.

But layers are not enough for automation.

A layer can tell a human that something is meant to be a building. It cannot reliably tell the software:

1. How many floors the building has
2. What each floor is used for
3. What its total built-up area is
4. Which parcel it sits on
5. What its FAR or coverage impact is
6. What should update if it changes

This is why automated workflows break so easily.

The drawing changes in one place. The consequences are checked somewhere else.

## The Metrics Already Exist

Architects and planners already calculate FAR, coverage, total built-up area and use mix.

These are not new ideas.

The problem is that they often live outside the design model.

They live in spreadsheets, schedules, manual take-offs, diagrams or disconnected scripts.

So when the design changes, someone has to check whether the numbers still make sense.

If a building gains two floors, total built-up area changes.

If the footprint grows, coverage changes.

If the lower floors switch from residential to retail, the use mix changes.

If the parcel boundary shifts, FAR changes.

These are not just drawing edits. They are planning decisions.

And if every planning decision creates a manual coordination task, then automation will never feel trustworthy.

## The Living Site Model

I think the missing piece is a living site model.

A living site model is different from a drawing.

A drawing shows what is there.

A living model knows what each thing is, how it relates to everything else, and what needs to update when it changes.

In a living site model:

1. A building is not just a mass. It has floors, uses, an envelope, area and height.
2. A parcel is not just a boundary. It can know what sits on it and what capacity it carries.
3. A road is not just a line. It can affect access, frontage, parcels and buildable land.
4. A zone is not just a boundary. It can know what buildings, parcels or roads fall inside it.
5. Metrics are not separate reports. They are calculated from the model.

This is the important shift.

Not better layer organisation.

Not prettier geometry.

But a model where the plan is made of things that know what they are.

## Orphaned Geometry vs Living Site Model

| Orphaned geometry workflow | Living site model |
| --- | --- |
| A building is a mass or surface on a layer | A building knows its floors, use, envelope and area |
| Metrics are checked separately | FAR, coverage, built-up area and use mix update from the model |
| A parcel is a boundary curve | A parcel can know what sits on it and what capacity it carries |
| A road is a drawn line or surface | A road can affect parcels, access, frontage and buildable land |
| AI sees shapes, layers and labels | AI can act on buildings, parcels, uses, metrics and constraints |
| Automation produces drawings | Automation updates consequences |

The value of this is not just neatness.

The value is causality.

When one part of the plan changes, the rest of the model can respond.

## Why This Matters For AI

AI is very good at interpreting language and images given enough context.

But if it is looking at orphaned geometry, it has to guess too much.

It has to guess what each shape means.

It has to guess which objects are related.

It has to guess which metrics matter.

It has to guess what should update when something changes.

That is too much magic to expect from AI.

The better approach is to give AI a model it can act on.

Not anonymous shapes.

But buildings, parcels, roads, zones, uses and metrics.

Then AI can become useful in a much more grounded way. It can suggest changes, compare options, explain consequences and help operate the workflow.

But the living site model needs to come first.

## First Milestone

The first milestone is not full automation of master planning.

The first milestone is making the core ingredients of a site plan live:

1. Buildings
2. Parcels
3. Roads
4. Zones
5. Uses
6. Metrics

Once these objects can update together, automation has something solid to build on.

This is the direction I am exploring with AIQ.

Not asking AI to magically understand drawings.

But creating a living site model that humans, automation and AI can all work through.

## Conclusion

AI cannot meaningfully automate site planning if the plan is still made of orphaned geometry.

Before we ask AI to design a masterplan, we need to give it something better to work with.

A living site model.

One where buildings, parcels, roads, zones, uses and metrics are connected.

This is the first step toward trustworthy automation and faster early-stage planning.

But this only solves the first problem.

The second problem is misplaced intelligence: expecting either parametric scripts or AI models to handle the whole planning process alone.

That is what I will explore next.

## Title Alternatives

1. AI Can't Site Plan Because Your CAD Model Doesn't Know What a Building Is
2. Your CAD Model Doesn't Know What A Building Is
3. AI For Site Plan Automation: The Problem With Orphaned Geometry
4. Automated Master Planning Starts With A Living Site Model
