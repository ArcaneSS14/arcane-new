## CVar change checklist

Use for every new or changed CVar. Select only checks relevant to the change.

- stable namespaced key with no existing collision;
- typed default;
- documented unit;
- valid bounds;
- correct server/client/archive/replication/notification flags for the owning assembly;
- runtime update behavior defined;
- change subscription cleaned up by the system subscription manager or explicit unsubscribe;
- config compatibility considered;
- dangerous, invalid, or non-finite numeric values rejected before use;
- secrets are not stored in CVars; `CONFIDENTIAL` is not a secret-storage mechanism.
