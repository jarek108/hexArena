import time
import sys

if __name__ == "__main__":
    if len(sys.argv) > 1:
        try:
            seconds = int(sys.argv[1])
            time.sleep(seconds)
            print(f"Waited {seconds} seconds.")
        except ValueError:
            print("Error: Please provide a valid integer for seconds to wait.")
    else:
        print("Usage: python wait.py <seconds>")