#!/usr/bin/env python3
# -*- coding:utf-8 -*-

from pyperclip import copy as copy

file_reader = open(r"C:\Users\rbald\OneDrive\Dokumente\Kursjahr 1\114 Kodierungs- Kompressions und Verschlüsselungsverfahren\Zus. Dokumente\02_Dateien\4.6\Beispiel4.6.1.rtf", "rb")
file_content = file_reader.read()

file_writer = open(r"C:\Users\rbald\OneDrive\Dokumente\Kursjahr 1\114 Kodierungs- Kompressions und Verschlüsselungsverfahren\Zus. Dokumente\02_Dateien\4.6\Beispiel.txt", "w", encoding="utf-8")
file_writer.write(str(file_content))
file_writer.close()
