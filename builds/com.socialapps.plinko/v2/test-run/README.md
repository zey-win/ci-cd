# Device launch evidence template

This folder follows the `test-run-YYYYMMDD-HHMMSS` evidence format requested by marketing.

Expected files:

- `am-start.txt` — Android activity start command output.
- `logcat.txt` — launch logcat excerpt.
- `logcat-35s.txt` — longer post-launch logcat excerpt.
- `screen.png` — first launch screenshot.
- `screen-35s.png` — later launch screenshot.
- `screen-after-launch.png` — available Plinko launch screenshot for this release report.
- `logcat-after-reinstall.txt` — available Plinko launch/reinstall log for this release report.

A 15-second real phone launch video should be added here as `phone-launch-com.socialapps.plinko-v2.mp4` when a device is available. No phone was available to ADB when this report was packaged.
