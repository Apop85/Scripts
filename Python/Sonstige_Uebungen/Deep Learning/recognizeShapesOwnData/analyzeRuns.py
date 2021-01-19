import os

os.chdir(os.path.dirname(__file__))

# Pfaddefinitionen
log_dir = os.path.join(".", "logs")
model_dir = os.path.join(".", "modelData")
x_file_dir = os.path.join(model_dir, "X.pickle")
y_file_dir = os.path.join(model_dir, "y.pickle")
error_log_path = os.path.join(log_dir, "error.log")
success_log_path = os.path.join(log_dir, "success.log")
bestruns_log_path = os.path.join(log_dir, "bestruns.log")

def write_to_log(path, message):
    file_writer = open(path, "a+")
    file_writer.write(f"{message}\n")
    file_writer.close()

def analyzeRuns(list_size=10):
    best_acc = {}
    best_loss = {}

    file_reader = open(success_log_path, "r")
    successfull_runs = file_reader.readlines()[2:]
    file_reader.close()

    # Extrahiere Daten aus Zeilen
    for i in range(len(successfull_runs)):
        line = successfull_runs[i]
        line = line.split("|")
        for j in range(len(line)):
            line[j] = line[j].strip(" ")
        successfull_runs[i] = line

    for run in successfull_runs:
        if not len(run) == 9:
            raise Exception(f"Run {successfull_runs.index(run)} contains not enough arguments")

        optimizer = run[0]
        decision_activator = run[1]
        loss_algorithm = run[2]
        activator = run[3]
        try:
            dense_layers = int(run[4])
            layer_size = int(run[5])
            convolution_layers = int(run[6])
            accuracy = float(run[7])
            loss = float(run[8])
        except:
            raise Exception(f"Run {successfull_runs.index(run)} contains invalid arguments")

        if len(best_acc.keys()) < list_size:
            best_acc.setdefault(str(accuracy), {"optimizer": optimizer, "decisionActivator": decision_activator, "lossAlgorithm": loss_algorithm, 
                                                           "activator": activator, "denseLayers": dense_layers, "layerSize": layer_size, 
                                                           "convolutionLayers": convolution_layers, "accuracy": accuracy, "loss": loss})
        else:
            for key in best_acc.keys():
                if accuracy > float(key):
                    del best_acc[key]
                    best_acc.setdefault(str(accuracy), {"optimizer": optimizer, "decisionActivator": decision_activator, "lossAlgorithm": loss_algorithm, 
                                                        "activator": activator, "denseLayers": dense_layers, "layerSize": layer_size, 
                                                        "convolutionLayers": convolution_layers, "accuracy": accuracy, "loss": loss})
                break



        if len(best_loss.keys()) < list_size:
            best_loss.setdefault(str(loss), {"optimizer": optimizer, "decisionActivator": decision_activator, "lossAlgorithm": loss_algorithm, 
                                                           "activator": activator, "denseLayers": dense_layers, "layerSize": layer_size, 
                                                           "convolutionLayers": convolution_layers, "accuracy": accuracy, "loss": loss})
        else:
            for key in best_loss.keys():
                if loss < float(key):
                    del best_loss[key]
                    best_loss.setdefault(str(loss), {"optimizer": optimizer, "decisionActivator": decision_activator, "lossAlgorithm": loss_algorithm, 
                                                    "activator": activator, "denseLayers": dense_layers, "layerSize": layer_size, 
                                                    "convolutionLayers": convolution_layers, "accuracy": accuracy, "loss": loss})
                    break

    best_acc_list = []
    best_loss_list = []

    for value in best_acc.keys():
        best_acc_list += [value]
    best_acc_list.sort(reverse=True)
    
    for value in best_loss.keys():
        best_loss_list += [value]
    best_loss_list.sort()

    if os.path.exists(bestruns_log_path):
        os.remove(bestruns_log_path)

    for value in best_acc_list:
        # log_message = optimizer.center(33) + "|" + loss_algorithm.center(33) + "|" + activator.center(33) + "|" + str(dense_layer).center(33) + "|" + str(layer_size).center(33) + "|" + str(conv_layer).center(33) + "|" 
        log_message = best_acc[value]["optimizer"].center(33) + "|" + best_acc[value]["decisionActivator"].center(33) + "|" + best_acc[value]["lossAlgorithm"].center(33) + "|" + best_acc[value]["activator"].center(33) + "|" + str(best_acc[value]["denseLayers"]).center(33) + "|" + str(best_acc[value]["layerSize"]).center(33) + "|" + str(best_acc[value]["convolutionLayers"]).center(33) + "|" + str(best_acc[value]["accuracy"]).center(33) + "|" + str(best_acc[value]["loss"]).center(33)
        if not os.path.exists(bestruns_log_path):
            header = "OPTIMIZER".center(33) + "|" + "DECISION ACTIVATOR".center(33) + "|" + "LOSS_ALG".center(33) + "|" + "ACTIVATOR".center(33) + "|" + "DENSE LAYERS".center(33) + "|" + "LAYER SIZE".center(33) + "|" + "CONVOLUTION LAYERS".center(33) + "|" + "ACCURRACY".center(33) + "|" + "LOSS".center(33) + "\n"
            header = header + "-"*len(header)+"\n"
            log_message = header + log_message
        write_to_log(bestruns_log_path, log_message)
    for value in best_loss_list:
        log_message = best_loss[value]["optimizer"].center(33) + "|" + best_loss[value]["decisionActivator"].center(33) + "|" + best_loss[value]["lossAlgorithm"].center(33) + "|" + best_loss[value]["activator"].center(33) + "|" + str(best_loss[value]["denseLayers"]).center(33) + "|" + str(best_loss[value]["layerSize"]).center(33) + "|" + str(best_loss[value]["convolutionLayers"]).center(33) + "|" + str(best_loss[value]["accuracy"]).center(33) + "|" + str(best_loss[value]["loss"]).center(33)
        write_to_log(bestruns_log_path, log_message)

# analyzeRuns(20)