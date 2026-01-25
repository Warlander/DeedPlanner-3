Don't write or execute tests unless asked for it.

Don't use nullable references unless asked for it. Prefer using dummy objects or structs instead.

Never make variables public, always use properties instead.

Use constructor dependency injection for dependencies. Add dependencies to installer if necessary. Injected dependencies should be private readonly. For MonoBehavior classes where constructor injection is not possible, use method with [Inject] attribute instead.

Favor readonly class fields if possible.

Always make [SerializedField] private. Prefer using [SerializeField] instead of GetComponent calls where applicable.