
Inspect `git status --short --branch` before staging or committing.

Do not run destructive commands without explicit approval for that exact action:

- `git reset --hard`;
- `git clean -fd` or `git clean -fdx`;
- `git checkout -- .`;
- `git restore .`;
- force push or history rewriting;
- recursive forced deletion;
- worktree deletion;
- removal of untracked files.

Keep unrelated local files out of commits. Do not silently amend, rebase, squash, or force-update a shared branch. When a push fails because the remote moved, report it instead of choosing a merge or rebase policy for the user.

Read-only commands such as status, diff, log, show, grep, and ls-files are safe defaults.

Vendored examples do not authorize destructive cleanup. Excluding `*.binlog` from `git clean` still leaves other untracked and ignored work at risk; use an existing binlog or a build log without cleaning by default. A cleanup request needs the exact approved operation, target, and preservation plan under this rule.

Before a checkout or worktree operation that must not initialize dependencies, inspect the applicable Git hooks. The repository's `post-checkout` can run `BuildChecker/git_helper.py`, update shared Git hooks, and initialize submodules. If setup is outside the task, suppress hooks for that one operation with a command-local `core.hooksPath` pointing to an empty directory; do not change shared hook files or persistent configuration to bypass them.
