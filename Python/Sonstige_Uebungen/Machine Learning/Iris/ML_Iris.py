#!/usr/bin/env python3
# -*- coding:utf-8 -*-

# 
####
# File: ML_Iris.py
# Project: Iris
#-----
# Created Date: Saturday 16.01.2021, 20:08
# Author: Apop85
#-----
# Last Modified: Saturday 16.01.2021, 20:11
#-----
# Copyright (c) 2021 Apop85
# This software is published under the MIT license.
# Check http://www.opensource.org/licenses/MIT for further informations
#-----
# Description:
# Source: https://www.g-webservice.de/python-machine-learning-tutorial/
####

import numpy as np
import pandas as pd
import matplotlib.pyplot as plt
import os
from sklearn.model_selection import train_test_split
from sklearn.linear_model import LogisticRegression
from sklearn.metrics import accuracy_score
from sklearn.tree import DecisionTreeClassifier


os.chdir(os.path.dirname(__file__))
print("Processing CSV...", end="")
csv_path = r'.\data\iris_data.csv'
# Iris Daten laden
df = pd.read_csv(csv_path)
print("Done")
print(f"Datashape: {df.shape}")

print("Create Plot...", end="")
# Klassenzugehörigkeit der Datenpunkte anhand der Farbe
colors = {'setosa' : 'r', 'versicolor' : 'g', 'virginica' : 'b'}
# Scatterplot für die Features Sepal length und Sepal width
fig, ax = plt.subplots(figsize=(10, 5))
for i in range(len(df['sepal_length'])):
    ax.scatter(df['sepal_length'][i], df['sepal_width'][i],color=colors[df['species'][i]])

ax.set_title('Iris Daten')
ax.set_xlabel('Sepal length')
ax.set_ylabel('Sepal width')
# Graph anzeiegen
fig.show()
print("Done")

species_to_int = {'setosa': 0, 'versicolor': 1, 'virginica': 2}
# Mappen der Spezies zu Zahlen
df['species'] = df['species'].map(species_to_int)

# unabhängige Variablen „X“ (Sepal length, Sepal width, Petal length und Petal width)
X = df.iloc[:,:-1]
# abhängige Variable „y“ (Species) 
y = df.iloc[:,-1]
print('X:', X.shape,' y:', y.shape)

# Daten in Trainings- und Testdaten aufteilen
# Mit den Testdaten können wir dann die Performance unserer Modelle bewerten.
# Input:
    # X: Featurematrix aller Daten
    # y: Zielvektor (Labels)
    # test_size: Prozentuale Größe des neuen Test-Datensatzes (X_test)
    # random_state: Zufallsparameter zur Reproduktion der Ergebnisse
# Output:
    # X_train: Trainingsdaten (Features)
    # y_train: Zielvektor (Labels) der Trainingsdaten
    # X_test: Testdaten (Features)
    # y_test: Zielvektor (Labels) der Testdaten
print("Training data...", end="")
X_train, X_test, y_train, y_test = train_test_split(X, y, test_size=0.2, random_state=42)
print("Done")
print('X_train:', X_train.shape, ' y_train:', y_train.shape)
print('X_test:', X_test.shape, ' y_test:', y_test.shape)

print("Training data (log)...", end="")
# initiieren und trainieren des Modells mit einer Logistischen Regression
log_clf = LogisticRegression()
# trainieren des Modells
log_clf.fit(X_train, y_train)
# Performance-Metrik evaluieren - Vorhersagen für alle Fälle im Testdatensatz generieren
y_pred = log_clf.predict(X_test)
# Accuracy berechnen
acc_score = accuracy_score(y_test, y_pred)
print("Done")
print('Accuracy:', acc_score)

# Accuracy Decision Tree-Modell
print("Training Data (tree)...", end="")
tree_clf = DecisionTreeClassifier()
tree_clf.fit(X_train, y_train)
print("Done")
print('Accuracy Decision Tree:', tree_clf.score(X_test, y_test))

# Test mit einem Decision Tree mit einer maximalen Tiefe von 1
print("Training Data (badtree)...", end="")
tree_clf = DecisionTreeClassifier(max_depth=1)
tree_clf.fit(X_train, y_train)
print("Done")
print('Accuracy Bad Decision Tree:', tree_clf.score(X_test, y_test))

input("DONE. Press Enter to exit script")