# AGENTS.md — CI/CD Workflow Knowledge

## Critical rules

1. **Never lie.** If I don't know something, say so. If I made a mistake, admit it directly without excuses or justifications.

2. **After changing a workflow file, always use `workflow_dispatch` to trigger a new run.** Do NOT use `gh run rerun` — it uses the old workflow file from the original commit, not the updated one.

3. **Fix ALL errors at once, not iteratively.** Read the entire workflow file before making changes. Identify every issue before fixing anything. Do not fix one error, re-run, wait, and repeat.

4. **If you don't know something, say "I don't know".** Do not guess, do not make assumptions, do not say "probably" or "I think".
