#!/usr/bin/env python3
# -*- coding:utf-8 -*-
###
# File: pdf_ocr.py
# Project: Sonstige_Uebungen
# Created Date: Sunday 02.06.2019, 17:30
# Author: Apop85
# -----
# Last Modified: Tuesday 04.06.2019, 11:24
# -----
# Copyright (c) 2019 Apop85
# This software is published under the MIT license.
# Check http://www.opensource.org/licenses/MIT for further informations
# -----
# Description: Extract text from PDF
###

import pytesseract
import os
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

file_counter=0
def open_pdf(file_list):
    # Open all pdf files and read out text
    global file_counter, pdf_file
    file_list_done=os.listdir(target_dir_txt)
    for pdf_file in file_list:
        file_counter+=1
        if pdf_file.endswith('pdf') and pdf_file[:-3]+'txt' not in file_list_done:
            percent_value=round(100/len(file_list)*file_counter, 1)
            print('Open file '+str(file_counter)+' of '+str(len(file_list))+' ('+str(percent_value)+'%)'+' : '+pdf_file)
            pdf2img(pdf_file)
        else:
            print('Skipped file: '+pdf_file)
            
def pdf2img(pdf_file):
    # Convert PDF into Images
    global target_dir_pdf, target_dir_img
    print('Convert PDF pages to images...', end='')
    images_from_path = convert_from_path(target_dir_pdf+'\\'+pdf_file,  output_folder=target_dir_img, fmt='jpg')
    print('Done')
    img2text(images_from_path)

def img2text(images):
    # Read out text from images 
    text, image_counter='', 0
    for image in images:
        image_counter+=1
        percent_value=round(100/len(images)*image_counter, 1)
        print('\r'*100+'Text recognition @ page: '+str(image_counter)+' of '+str(len(images))+' ('+str(percent_value)+'%)', end='')
        text += pytesseract.image_to_string(image)
    write_txt_file(text)

def write_txt_file(text):
    # Write resulting file content to txt file
    print('\nWrite file: '+target_dir_txt+'\\'+pdf_file[:-3]+'txt')
    text_file=open(target_dir_txt+'\\'+pdf_file[:-3]+'txt', 'w', encoding='UTF-8')
    text_file.write(text)
    text_file.close()
    clear_tmp_images()

def clear_tmp_images():
    # Remove all images
    image_counter=0
    image_list=os.listdir(target_dir_img)
    for image in image_list:
        image_counter+=1
        percent_value=round(100/len(image_list)*image_counter, 1)
        print('\r'*100+'Clear images: '+str(image_counter)+' of '+str(len(image_list))+' ('+str(percent_value)+'%)', end='')
        os.remove(target_dir_img+'\\'+image)
    print('\n\n\n')

init_script()
file_list=get_filenames(target_dir_pdf)
open_pdf(file_list)

print('Converting complete! Press enter to exit')
input()
