import os

#  _______  _______ ___________________________ _        _______  _______ 
# (  ____ \(  ____ \\__   __/\__   __/\__   __/( (    /|(  ____ \(  ____ \
# | (    \/| (    \/   ) (      ) (      ) (   |  \  ( || (    \/| (    \/
# | (_____ | (__       | |      | |      | |   |   \ | || |      | (_____ 
# (_____  )|  __)      | |      | |      | |   | (\ \) || | ____ (_____  )
#       ) || (         | |      | |      | |   | | \   || | \_  )      ) |
# /\____) || (____/\   | |      | |   ___) (___| )  \  || (___) |/\____) |
# \_______)(_______/   )_(      )_(   \_______/|/    )_)(_______)\_______)
# Pfaddefinitionen
log_dir = os.path.join(".", "logs")


#  _______           _        _______ __________________ _______  _        _______ 
# (  ____ \|\     /|( (    /|(  ____ \\__   __/\__   __/(  ___  )( (    /|(  ____ \
# | (    \/| )   ( ||  \  ( || (    \/   ) (      ) (   | (   ) ||  \  ( || (    \/
# | (__    | |   | ||   \ | || |         | |      | |   | |   | ||   \ | || (_____ 
# |  __)   | |   | || (\ \) || |         | |      | |   | |   | || (\ \) |(_____  )
# | (      | |   | || | \   || |         | |      | |   | |   | || | \   |      ) |
# | )      | (___) || )  \  || (____/\   | |   ___) (___| (___) || )  \  |/\____) |
# |/       (_______)|/    )_)(_______/   )_(   \_______/(_______)|/    )_)\_______)
def write_to_log(path, message):
    # Funktion um einen String in ein Logfile zu schreiben
    # Inputs:
    #   path = string | Pfad zum Logfile
    #   message = string | Nachricht
    file_writer = open(path, "a+")
    file_writer.write(f"{message}\n")
    file_writer.close()

def analyzeRuns(model_name="", list_size=10):
# def analyzeRuns(list_size=10):
    # Funktion zur erstellen einer Top-Liste
    # Inputs:
    #   list_size = integer | Anzahl der Top-Runs in der Liste

    # success_log_path = os.path.join(log_dir, f"success.log")
    # bestruns_log_path = os.path.join(log_dir, f"bestruns.log")
    success_log_path = os.path.join(log_dir, f"success-{model_name}.log")
    bestruns_log_path = os.path.join(log_dir, f"bestruns-{model_name}.log")
    
    # Setze Standardwerte
    best_acc = {}
    best_loss = {}

    # Lese Logfile aus
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

    # Prüfe ob das Logfile die richtige Anzahl Datensätze pro Zeile enthält
    for run in successfull_runs:
        if not len(run) == 11:
            raise Exception(f"Run {successfull_runs.index(run)} contains not enough arguments")
        
        # Speichere Datensätze in Variabel
        optimizer = run[0]
        kernel_size = run[1]
        decision_activator = run[2]
        loss_algorithm = run[3]
        activator = run[4]
        try:
            dense_layers = int(run[5])
            dense_activator = run[6]
            layer_size = int(run[7])
            convolution_layers = int(run[8])
            accuracy = float(run[9])
            loss = float(run[10])
        except:
            raise Exception(f"Run {successfull_runs.index(run)} contains invalid arguments")

        # Erfasse höchste Genauigkeit
        if len(best_acc.keys()) < list_size:
            best_acc.setdefault(str(accuracy), {"optimizer": optimizer, "decisionActivator": decision_activator, "lossAlgorithm": loss_algorithm, 
                                                "activator": activator, "denseLayers": dense_layers, "layerSize": layer_size, 
                                                "convolutionLayers": convolution_layers, "accuracy": accuracy, "loss": loss,
                                                "kernelSize": kernel_size, "denseActivator": dense_activator})
        else:
            for key in best_acc.keys():
                if accuracy > float(key):
                    # Ist die Genauigkeit besser als der aktuelle Prüfwert füge neue Werte ein
                    best_acc.setdefault(str(accuracy), {"optimizer": optimizer, "decisionActivator": decision_activator, "lossAlgorithm": loss_algorithm, 
                                                        "activator": activator, "denseLayers": dense_layers, "layerSize": layer_size, 
                                                        "convolutionLayers": convolution_layers, "accuracy": accuracy, "loss": loss,
                                                        "kernelSize": kernel_size, "denseActivator": dense_activator})
                    sorted_keys = []
                    for key in best_acc.keys():
                        sorted_keys += [key]
                    sorted_keys.sort()
                    if len(sorted_keys) >= list_size:
                        # Werfe schlechtesten Wert raus
                        del best_acc[sorted_keys[-1]]
                break


        # Erfasse niedrigster Verlust
        if len(best_loss.keys()) < list_size:
            best_loss.setdefault(str(loss), {"optimizer": optimizer, "decisionActivator": decision_activator, "lossAlgorithm": loss_algorithm, 
                                            "activator": activator, "denseLayers": dense_layers, "layerSize": layer_size, 
                                            "convolutionLayers": convolution_layers, "accuracy": accuracy, "loss": loss,
                                            "kernelSize": kernel_size, "denseActivator": dense_activator})
        else:
            for key in best_loss.keys():
                if loss < float(key):
                    # Ist der Verlust kleiner als der aktuelle Prüfwert füge neue Werte ein
                    best_loss.setdefault(str(loss), {"optimizer": optimizer, "decisionActivator": decision_activator, "lossAlgorithm": loss_algorithm, 
                                                    "activator": activator, "denseLayers": dense_layers, "layerSize": layer_size, 
                                                    "convolutionLayers": convolution_layers, "accuracy": accuracy, "loss": loss,
                                                    "kernelSize": kernel_size, "denseActivator": dense_activator})
                    sorted_keys = []
                    for key in best_loss.keys():
                        sorted_keys += [key]
                    sorted_keys.sort(reverse=True)
                    if len(sorted_keys) >= list_size:
                        # Werfe schlechtesten Wert raus
                        del best_loss[sorted_keys[-1]]
                    break

    best_acc_list = []
    best_loss_list = []

    # Erstelle Bestenlisten
    for value in best_acc.keys():
        best_acc_list += [value]
    best_acc_list.sort(reverse=True)
    
    for value in best_loss.keys():
        best_loss_list += [value]
    best_loss_list.sort()

    # Entferne vorheriges logfile
    if os.path.exists(bestruns_log_path):
        os.remove(bestruns_log_path)

    for value in best_acc_list:
        # log_message = optimizer.center(33) + "|" + loss_algorithm.center(33) + "|" + activator.center(33) + "|" + str(dense_layer).center(33) + "|" + str(layer_size).center(33) + "|" + str(conv_layer).center(33) + "|" 
        log_message = best_acc[value]["optimizer"].center(33) + "|" + best_acc[value]["kernelSize"].center(33) + "|" + best_acc[value]["decisionActivator"].center(33) + "|" + best_acc[value]["lossAlgorithm"].center(33) + "|" + best_acc[value]["activator"].center(33) + "|" + str(best_acc[value]["denseLayers"]).center(33) + "|" + best_acc[value]["denseActivator"].center(33) + "|" + str(best_acc[value]["layerSize"]).center(33) + "|" + str(best_acc[value]["convolutionLayers"]).center(33) + "|" + str(best_acc[value]["accuracy"]).center(33) + "|" + str(best_acc[value]["loss"]).center(33)
        if not os.path.exists(bestruns_log_path):
            header = "OPTIMIZER".center(33) + "|" + "KERNEL SIZE".center(33) + "|" + "DECISION ACTIVATOR".center(33) + "|" + "LOSS_ALG".center(33) + "|" + "ACTIVATOR".center(33) + "|" + "DENSE LAYERS".center(33) + "|" + "DENSE ACTIVATOR".center(33) + "|" + "LAYER SIZE".center(33) + "|" + "CONVOLUTION LAYERS".center(33) + "|" + "ACCURRACY".center(33) + "|" + "LOSS".center(33) + "\n"
            header = header + "-"*len(header)+"\n"
            log_message = header + log_message
        write_to_log(bestruns_log_path, log_message)

    write_to_log(bestruns_log_path, "'-_-" * (len(log_message) // 4))
    for value in best_loss_list:
        log_message = best_loss[value]["optimizer"].center(33) + "|" + best_loss[value]["kernelSize"].center(33) + "|" + best_loss[value]["decisionActivator"].center(33) + "|" + best_loss[value]["lossAlgorithm"].center(33) + "|" + best_loss[value]["activator"].center(33) + "|" + str(best_loss[value]["denseLayers"]).center(33) + "|" + best_loss[value]["denseActivator"].center(33) + "|" + str(best_loss[value]["layerSize"]).center(33) + "|" + str(best_loss[value]["convolutionLayers"]).center(33) + "|" + str(best_loss[value]["accuracy"]).center(33) + "|" + str(best_loss[value]["loss"]).center(33)
        write_to_log(bestruns_log_path, log_message)

