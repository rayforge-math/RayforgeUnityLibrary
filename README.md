# Rayforge Unity Library

A collection of personal Unity utilities, wrappers, extension methods, and helper systems designed to streamline common tasks and reduce boilerplate code.

## Design Philosophy

Some traditional OOP concepts, such as deep inheritance hierarchies, interfaces, and extensive use of virtual members, are **intentionally minimized** in this library. This is primarily for performance reasons in real-time 3D rendering. Examples of considerations include:

- **Virtual members and interfaces** introduce vtable lookups, preventing the compiler from resolving member access as a simple `this ptr + offset`.
- **Heap allocations** incur kernel calls and additional latency.
- **Pointer-based composition** can result in cache misses when traversing scattered objects.

To mitigate these issues, parts of the library are **kept as flat and modular as possible**:

- Systems focus on a single responsibility, making them easier to reuse or swap.
- Flatter, type-specific designs reduce runtime indirection and improve predictability in hot paths.
- Modules are structured to allow high-performance access, which is crucial when summing thousands of operations per frame in 3D real-time rendering.

These are just a few examples of the design choices made; there are certainly other aspects considered to ensure **efficiency, clarity, and maintainability**. The library aims to balance modularity with performance, consciously avoiding unnecessary OOP overhead where it matters most.

## Installation

1. Open your Unity project.
2. Navigate to **Window → Package Manager**.
3. Click the **`+`** button in the top-left corner.
4. Select **“Install package from Git URL…”**.
5. Paste the repository URL and confirm.
