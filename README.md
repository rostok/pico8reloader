# pico8reloader
pico8reloader will watch folder for any p8 file changes.
in case of file write or file rename it will:
1) run pico8 with latest p8 file if it is not running (after delay or check)
2) restart pico8 if lastest p8 file is not in command line
3) sent Ctrl+R (reload) keystroke to pico8 process
4) on --focus keep focus on pico8 window, or get to previous one

syntax: ```pico8reloader [path] [--winpos=x,y[,w,h]] [--focus] [--delay=milisecs] [--check]```



this comes with MIT license from rostok - https://github.com/rostok/


default path is ```.```

winpos sets default pico8 position, no w/h values assumes 256

pico8 should be accessible via PATH variable (.bat or a shim)

this tool runs only on windows as it relies on PostMessage and WM_KEYUP, WM_KEYDOWN

# changes
* 1.0.3.0 - Added --delay to wait for cloud backup file syncing
* 1.0.4.0 - Added --check and fixed active windows size being changed by SW_RESTORE by setting it to  previous size and pos 

# VS Code integration
pico reloader can be easily integrated with VS Code via tasks.json

here's a sample configuration that will run pico8reloader with Ctrl+B shortcut

```json
{
    "version": "2.0.0",
    "tasks": [
        {
            "label": "start pico8reloader",
            "type": "shell",
            "command": "/path/to/pico8reloader.exe .",
            "group": {
                "kind": "build",
                "isDefault": true
            }
        }
    ]
}
```

# advanced tasks.json
This ```tasks.json``` setup will, on each execution (started by Ctrl+Shift+B), kill old pico8reloader and also precisely set size of VS Code. Note that [NirCmd](https://nircmd.nirsoft.net/) is required. 

```json
{
    "version": "2.0.0",
    "tasks": [
        {
            "label": "kill-pico8reloader",
            "type": "shell",
            "command": "taskkill /f /im pico8reloader.exe",
        },
        {
            "label": "pico8reloader",
            "type": "shell",
            "command": "taskkill /f /im pico8reloader.exe ; C:/projects/c#/pico8reloader/pico8reloader.exe . --winpos=1522,0,406,430",
        },
        {
            "label": "code-resize",
            "type": "shell",
            "command": "nircmd win max ititle Visual~x20Studio~x20Code ; nircmd win togglemax ititle Visual~x20Studio~x20Code ; nircmd win setsize ititle Visual~x20Studio~x20Code 0 0 1530 1050",
        },
        {
            "label": "pico8reloader and resize code",
            "dependsOn": ["code-resize","pico8reloader"],
            "group": {
                "kind": "build",
                "isDefault": true
            }
        }
    ]
}
```

# license
MIT
