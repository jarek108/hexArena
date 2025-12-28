import ctypes
from ctypes import wintypes

kernel32 = ctypes.windll.kernel32

# Define required structs and types
class COORD(ctypes.Structure):
    _fields_ = [("X", wintypes.SHORT), ("Y", wintypes.SHORT)]

print('trying')

# Win32 Constants
STD_OUTPUT_HANDLE = -11
INVALID_HANDLE_VALUE = -1

kernel32.FreeConsole()

print('trying')
pid = 6836

# 2. Attach to the target process console
if not kernel32.AttachConsole(pid):
    raise Exception(f"Could not attach to PID {pid}. Error: {ctypes.get_last_error()}")


print('trying')

# 3. Get the handle to the output buffer
h_stdout = kernel32.GetStdHandle(STD_OUTPUT_HANDLE)
if h_stdout == INVALID_HANDLE_VALUE:
    raise Exception("Invalid StdOut Handle")

# 4. Get buffer info (dimensions)
csbi = ctypes.create_string_buffer(22) # CONSOLE_SCREEN_BUFFER_INFO
if not kernel32.GetConsoleScreenBufferInfo(h_stdout, csbi):
    raise Exception("Could not get buffer info")

# Parse width/height from struct (simplified)
width = int.from_bytes(csbi[0:2], 'little')
height = int.from_bytes(csbi[2:4], 'little')

# 5. Read the content
length = width * height
buffer = ctypes.create_unicode_buffer(length)
read_count = wintypes.DWORD()
coord = COORD(0, 0) # Start from top-left

success = kernel32.ReadConsoleOutputCharacterW(
    h_stdout, buffer, length, coord, ctypes.byref(read_count)
)

kernel32.FreeConsole()

if not success:
    raise Exception(f"Failed to read console. Error: {ctypes.get_last_error()}")