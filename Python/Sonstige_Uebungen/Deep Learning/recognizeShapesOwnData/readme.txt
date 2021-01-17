==============================================================
Train triangle, square and circle shapes with a neural network
==============================================================

========= IMPORTANT =========
Only works with Python x64
Only uses CPU for training

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
-- INFO: Script creates 3500 train images per shape
-- INFO: Script creates 3 test images per shape to show at the end
2. Start TensorBoard with cmd command: 
-- tensorboard --logdir=<PFAD ZUM LOG-ORDNER>
-- Open webbrowser: http://localhost:6006
3. Run optimizeLearning.py to check possible model improvements
-- Default: 100 Iterations on 4 different Models

