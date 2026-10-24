# Git safety and metadata

Inspect status and the relevant diff before writing, staging or committing. Preserve unrelated tracked and untracked work. Stage only reviewed task paths; do not stage a path or bulk git add -u selection you have not inspected.

Destructive discard/deletion, history rewriting, amend/rebase of shared work and force push require explicit authorization for that action. Do not run reset --hard, clean -fd/-fdx, checkout -- ., restore ., forced recursive cleanup or worktree deletion as routine preparation.

Follow the requested commit count and scope. Permission to commit or push does not permit merge. Only the user merges. A remote advancing or push failing does not authorize choosing merge/rebase/pull strategy or changing remotes; report the concrete state.

Never add, edit, remove, reorder, normalize, copy or generate a line containing SPDX- in code, resources or configuration unless the current request specifies that exact change. Preserve existing notices during instruction maintenance. Files under .agents/, .claude/, .cursor/ and .codex/ carry no SPDX headers; absence of a header is not permission to relicense or privately redistribute their content.
