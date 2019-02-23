#!/usr/bin/env python3
# -*- coding:utf-8 -*-
###
# File: 07_kapitel_05_repetitionsaufgabe_06_loan_repayment.py
# Project: Kapitel_05_Kontrollstrukturen
# Created Date: Saturday 23.02.2019, 22:54
# Author: Apop85
# -----
# Last Modified: Saturday 23.02.2019, 23:20
# -----
# Copyright (c) 2019 Apop85
# This software is published under the MIT license.
# Check http://www.opensource.org/licenses/MIT for further informations
# -----
# Description: Task 6. Page 146. This script should calculate how much someone have to pay off
# each year to pay back his loan including pay interest.
###

while True:
    loan_amount=input('How much loan you want to get? ')
    if loan_amount.isdecimal() and int(loan_amount) > 0:
        loan_amount=int(loan_amount)
        break

while True:
    pay_interest=input('Pay interest in percent per year: ')
    if pay_interest.isdecimal() and int(pay_interest) > 0:
        pay_interest=int(pay_interest)
        break

while True:
    yearly_amount=input('How much you want to pay back per year: ')
    if yearly_amount.isdecimal() and int(yearly_amount) > 0:
        yearly_amount=int(yearly_amount)
        break

rest_amount,year=loan_amount,1
while rest_amount != 0:
    pay_interest_amount=round(rest_amount/100*pay_interest)
    if rest_amount < yearly_amount:
        yearly_amount=rest_amount+pay_interest_amount
    rest_amount-=yearly_amount+pay_interest_amount
    if rest_amount < 0:
        rest_amount=0
    print('Year '+str(year)+'\tPaid: '+str(yearly_amount)+'\tPay interest: '+str(pay_interest_amount)+'\tRepayment: '+str(round(yearly_amount-pay_interest_amount))+'\tRest: '+str(rest_amount))
    year+=1

