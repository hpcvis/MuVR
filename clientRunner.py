import subprocess
import tkinter as tk
from tkinter import filedialog, simpledialog

root = tk.Tk()
root.withdraw()

client = filedialog.askopenfilename(title='Select Client File')
print(client)
clientNum = simpledialog.askinteger('How many clients?', "How many clients?")
print(clientNum)

for i in range(0, clientNum):
    subprocess.Popen([client])