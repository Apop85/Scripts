==============================================================
Train triangle, square and circle shapes with a neural network
--------------------------------------------------------------
This script set creates training images containing triangles
squares and circles. Those images where taken to train the 
model to be able to differentiate between them.
==============================================================

========= IMPORTANT =========
Only works with Python x64
Only uses CPU for training

================== SCRIPTS ==================
recognizeShape.py 
	Creates training data, train model, create test data, test against test data

optimizeLearning.py 
	run recognizeShape.py first 
	3 different algorithms for testing

optimizeModel.py 
	run recognizeShape.py first 
	Iterates trough different model settings to help you find the optimal model attributes

analyzeRuns.py
	Used by optimizeModel.py
	Uses Logfiles from optimizeModel to find the best loss values

=============== MODULES ===============
Used Python modules (install with PIP)
- tensorflow
- pickle
- pillow
- numpy
- mathplotlib
- opencv-python

==================== RUN ORDER ====================
1. Run recognizeShape.py to create training data
-- To recreate data delete unwanted subfolders or set debug=True
-- INFO: Script creates images for training
-- INFO: Script creates test images to check against the trained model
-- INFO: Change values inside script

2. Start TensorBoard with cmd command: 
-- tensorboard --logdir=<PFAD ZUM LOG-ORDNER>
-- Open webbrowser: http://localhost:6006

3. Run optimizeModel.py to check possible model improvements
-- TIP: Run a low amount of epochs first and increase as less settings you test with
-- INFO: Change values inside script

