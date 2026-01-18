# Feature: Exception Dialog and Error Logging

## The Problem

There are users reporting the same issue:

- "i still have the same problem, no matter what i click on it just closes down the programme. i really cant work out why it doing it."
- "I am having the same issue. I tried to open it via admin and regular, and no matter what, it closes seconds after it is opened, and anything in the app is pressed. "

## Possible Causes

I have been unable to reproduce this issue myself, even if I delete the 
`config.json` to simulate an invalid initial state. I believe this cause
can now be eliminated.

The next logical reason is that something causes an exception. Since there is no 
specific exception handling in place, from a user's perspective, it causes the 
app to close.

## The Solution

To address this, two things will be implemented to make it possible to identify
the root cause of the problem.

### 1. A global exception handler

This handler will capture any unhandled exceptions and display an error dialog 
to the user. This dialog will provide information about the error and possible 
steps to resolve it.

#### The Error Dialog

The dialog must be shown **AFTER** the error has been fully logged, so the
error log files are already available. It will contain the following elements:

- **Title**: "An unexpected error occurred"
- **Message**: A brief description of the error.
- **Log file location**: A button to open the app folder where error logs are stored.
- **Bug report link**: A link to the GitHub issues page to report errors.

> The Bug Report link is the following URL (place this in a constant):
> https://github.com/Mistralys/starfield-load-order-manager/issues

#### Dialog Styling

The dialog must follow Material Design v5 guidelines like all other windows.

### 2. Error logging 

Whenever an exception occurs, a logging functionality will record the exception 
details to a log file, including all application state information that's
available at that point without causing further issues. Ideally the same information
than that provided by the "Debug" menu "Copy Debug Info" option.

This log file can then be used for troubleshooting and support purposes, for example
by attaching it to a GitHub issue when reporting a bug.

