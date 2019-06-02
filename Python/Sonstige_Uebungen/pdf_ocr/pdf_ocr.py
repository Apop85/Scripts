#!/usr/bin/env python3
# -*- coding:utf-8 -*-
###
# File: pdf_ocr.py
# Project: Sonstige_Uebungen
# Created Date: Sunday 02.06.2019, 17:30
# Author: Apop85
# -----
# Last Modified: Sunday 02.06.2019, 19:23
# -----
# Copyright (c) 2019 Apop85
# This software is published under the MIT license.
# Check http://www.opensource.org/licenses/MIT for further informations
# -----
# Description: Extract text from PDF
###

import pytesseract
import os
import tempfile
from PIL import Image
from pdf2image import convert_from_path

from pdf2image.exceptions import (
    PDFInfoNotInstalledError,
    PDFPageCountError,
    PDFSyntaxError
)

# Set target directories
target_dir_img = r'.\images'
target_dir_pdf = r'.\pdf'
target_dir_txt = r'.\txt'
tesseract_cmd = r'C:\Program Files (x86)\Tesseract-OCR\tesseract'

def init_script():
    # Initialize conditions
    pytesseract.pytesseract.tesseract_cmd = tesseract_cmd
    os.chdir(os.path.dirname(__file__))

def get_filenames(dirname):
    # Get a list of all files in target dir
    file_list=os.listdir(dirname)
    return file_list

def open_pdf(file_list):
    # Open all pdf files and read out text
    global pdf_file
    for pdf_file in file_list:
        pdf2img(pdf_file)

def pdf2img(pdf_file):
    # Convert PDF into Images
    global target_dir_pdf, target_dir_img
    images_from_path = convert_from_path(target_dir_pdf+'\\'+pdf_file,  output_folder=target_dir_img, fmt='jpg')
    img2text(images_from_path)

def img2text(images):
    # Read out text from images 
    text=''
    for image in images:
        text += pytesseract.image_to_string(image)
    write_txt_file(text)

def write_txt_file(text):
    # Write resulting file content to txt file
    text_file=open(target_dir_txt+'\\'+pdf_file[:-3]+'txt', 'w')
    text_file.write(text)
    text_file.close()
    clear_tmp_images()

def clear_tmp_images():
    # Remove all images
    image_list=os.listdir(target_dir_img)
    for image in image_list:
        os.remove(target_dir_img+'\\'+image)

init_script()
file_list=get_filenames(target_dir_pdf)
open_pdf(file_list)




