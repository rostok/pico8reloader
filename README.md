# pico8reloader
pico8reloader will watch a folder for any .p8 file changes
in case of file write or file rename it will:
1) run pico8 with latest p8 file if it is not running
2) restart pico8 if lastest p8 file is not in command line and reposition new window to the old one position
3) send Ctrl+R (reload) keystroke to pico8 process

syntax: ```pico8reloader [path] [--winpos=x,y[,w,h]]```

default path is .
winpos sets default pico8 position, no w/h values assumes 256

pico8 executable should be accessible via PATH variable

this tool runs only on windows as it relies on PostMessage and WM_KEYUP, WM_KEYDOWN

# VS Code integration
pico reloader can be easily integrated with VS Code via tasks.json

here's a sample configuration that will run pico8reloader with Ctrl+B shortcut

```{
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

# license
MIT
