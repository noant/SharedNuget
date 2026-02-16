1. Chat language and character set
- Always reply in Russian in chat.
- Do not use emojis or unusual/exotic Unicode characters in code, logs, documentation, or messages (stick to plain ASCII where practical).

2. Questions first, edits second
- When the user asks a question, answer it first without modifying any files.
- Only propose code changes if they are truly needed after the answer (and keep proposals minimal).
- Keep information concise in both chat and documentation, but don't leave out important points when describing. - Translate into English.

3. No automatic build/run; concise change recap
- Do not build, run, or "verify by compiling" after making changes unless the user explicitly asks for it.
- After code changes are made, summarize the final changes in chat very concisely (what changed and where), without extra narration.

4. Error handling policy
- Prefer throwing exceptions on failures instead of returning `bool`, `null`, or error codes.
- Do not add blanket `try/catch` everywhere; trust underlying layers to throw meaningful exceptions.
- Only introduce `TryXxx(...)` patterns or `Result`-style wrappers if the user explicitly requests non-throwing APIs.
- Never swallow exceptions; try to not use rethrow and exceptions wrapping. If wrapping is necessary, preserve the original exception as `InnerException` and add actionable context (e.g., id, endpoint, status code).

5. Documentation, comments, and text output
- Do not create or update documentation files (README, USAGE, etc.) unless explicitly requested.
- Write comments, documentation text, log messages, and any "text inside code blocks" only when explicitly requested, and strictly in English (even if the existing codebase contains non-English comments).

6. Fluent code style preference
- Prefer declarative style over imperative style when it improves code clarity and maintainability.
- Prefer fluent code style patterns such as LINQ method chains, EF Core fluent API, fluent builders, and method chaining where applicable.
- Use LINQ methods (Where, Select, OrderBy, etc.) instead of imperative loops when they improve readability.
- Prefer fluent configuration APIs (e.g., EF Core model configuration, HttpClient configuration) over imperative style when available.
- Use fluent builders for complex object construction when it improves code clarity and maintainability.
  
7. Dependency Injection preference
- Prefer using Dependency Injection (DI) for instantiating classes instead of direct instantiation with `new`.
- Inject dependencies through constructor parameters rather than creating dependencies inside classes.
- Register services in the DI container and resolve them through constructor injection.
- Only use `new` for simple value objects, DTOs, or when creating objects that are not part of the application's dependency graph.

8. C# .NET preference for tooling and automation
- Prefer using C# .NET code for all tasks, automation, and tooling instead of PowerShell, Bash, or other scripting languages.
- Use C# console applications, scripts, or tools when automation or file operations are needed.
- Only use PowerShell, Bash, or other scripting languages when the user explicitly requests them or when they are required for specific platform integration.

9. Tools and MCP
- Always use Context7 MCP when I need library/API documentation, code generation, setup or configuration steps without me having to explicitly ask.

10. Project Structure
- Folders and subfolders are project units
- The root project name can be a proper name, for example LovelyApp.*
- Subprojects are represented as folders, can have their own proper names and can have their own entry point unit, for example projects LovelyApp.Walle.* will be responsible for software that runs on a mini-robot on Linux, and I named this subproject Walle.
- An entry point (EntryPoint) can be inside a project or subproject, responsible for an application that launches the application (WebApi, Console, WinApp), for example LovelyApp.Client.EntryPoint.Console is responsible for the client's console application.
- Processors (for example LovelyApp.Client.Processor) connect abstractions and execute them according to a specific algorithm; essentially, they are programmatic descriptions of UseCases.
- Intermediate names in a project can indicate terminology clarification, for example specifying what is responsible for a document search service, for instance the project LovelyApp.Search.PostgreSQL is responsible for the search provider in PostgreSQL.
- Projects for storing abstractions are marked with the name Abstractions, for example LovelyApp.Search.Abstractions will store interfaces, among which will be an interface whose implementation will be in LovelyApp.Search.PostgreSQL.