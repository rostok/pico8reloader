# pico8reloader
pico8reloader will watch a folder for any .p8 file changes
in case of file write or file rename it will:
1) run pico8 with latest p8 file if it is not running
2) restart pico8 if lastest p8 file is not in command line
3) sent Ctrl+R (reload) keystroke to pico8 process

syntax: pico8reloader path

default path is .

pico8 executable should be accessible via PATH variable

this tool runs only on windows as it relies on PostMessage and WM_KEYUP, WM_KEYDOWN

MIT license
