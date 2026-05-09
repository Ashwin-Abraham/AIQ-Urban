# What happens when an architect stops drawing and starts coding with AI

I am an architect. I am also a software engineer. For years these two identities lived in separate boxes — the parametric modeler on one screen, the IDE on the other. But something shifted recently.

I started using AI not just to write code faster, but to *design software faster* — to compress the loop between "I wonder if this would work" and "let me test it." This week I spent an afternoon in a structured interview with an AI agent, walking through the design of a new urban modelling plugin for Rhino 8. Every ambiguous term ("is a Use an enum or a class?") was caught and resolved in seconds. Every dependency decision ("should Core depend on RhinoCommon?") was stress-tested against concrete scenarios on the spot.

What would have taken days of whiteboarding and back-and-forth was done in a couple of hours, with a living document (CONTEXT.md) that captures every decision as it was made.

---

**Why does this matter?**

Because I am building something that goes beyond parametric modelling. I am building the low-level blocks of a city sandbox — an in-memory model of buildings, transit networks, parcels, zones, topography, and vegetation that lives inside Rhino. A model where adding a road automatically recalculates the parcels it intersects. Where every element knows its neighbours. Where the geometry kernel is fast and the data model is clean.

And this sandbox has a specific purpose: to be the playground for agentic city builders.

Imagine an AI agent that can propose urban designs — not by generating images, but by issuing commands into a structured environment. `AddBuildingOnParcel(...)`, `AddTransit(...)`, `ChangeUse(...)`. The sandbox handles the geometry. The sandbox resolves the dependencies. The agent never touches a curve or a mesh. It only sees the results: metrics, conflicts, affordances.

Act. Measure. React. The model is the interface.

---

This is early work. The codebase is fresh, the types are still taking shape, and the agent loop is a vision not yet built. But the foundation is being laid with intention: a clean domain model, an eager pipeline that resolves side effects automatically, and a persistent design record that an AI and a human can share.

If you are an architect who codes, or a developer who thinks about cities — this is the most interesting intersection I have found in years.

#Architecture #GenerativeDesign #UrbanPlanning #Rhino3D #AIAgents #SoftwareDesign
