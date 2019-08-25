import matplotlib.pyplot as plt
import numpy as np
import pandas as pd
from difflib import SequenceMatcher
from collections import Counter

import urllib.request


def ReplaceFrequent(a, b):
    a_clean = a.replace('-', ' ').replace(',', ' ').replace('/', ' ')
    for ii in range(0, 20):
        a_clean = a_clean.replace(b[ii], '')
    lstofWords=a_clean.split()
    lstofWords.sort()
    result=' '.join(lstofWords)
    return result


def similar(a, b):
    return SequenceMatcher(None, a, b).ratio()


def contains(testStr, exceptions):
    res = False
    for k in range(0, len(exceptions)):
        if (testStr == exceptions[k]):
            res = True
            break
    return res


def file():
    url = 'https://raw.githubusercontent.com/FelixKras/CSVParserForDataReduction/master/Data/operators.txt'
    urllib.request.urlretrieve(url, filename='operators.txt')

    file = open('operators.txt', 'r')
    col = []
    exceptions = ['Burma', 'Ethiopian', 'U.S.', 'US', 'Turkish', 'Royal', 'American', 'Afghan', 'Indonesia', 'Russian',
                  'Pacific', 'Soviet', 'Indian', 'British', 'Philippine', 'China', 'Pakistan']
    data = file.readlines()
    for i in range(0, len(data) - 1):
        sublist = data[i].replace('-', ' ').replace(',', ' ').replace('/', ' ').split()
        for j in range(0, len(sublist) - 1):
            if (not contains(sublist[j], exceptions)):
                col.append(sublist[j].lower())

    return col


def hist(col):
    handspan = []
    for i in range(11):
        handspan.append(0)
    for i in (col):
        handspan[i] += 1
    return handspan


col = file()
counts = Counter(col)
labels, values = zip(*counts.items())

# sort your values in descending order
indSort = np.argsort(values)[::-1]

# rearrange your data
labels = np.array(labels)[indSort]
values = np.array(values)[indSort]

indexes = np.arange(len(labels))
bar_width = 0.35

#plt.bar(indexes[:20], values[:20])
## add labels
#plt.xticks(indexes[:20] + bar_width, labels[:20])
#plt.show()

Data = pd.read_csv('https://raw.githubusercontent.com/FelixKras/CSVParserForDataReduction/master/Data/NewCSV.csv')
Data.insert(17, 'Survivability', Data["Total Fatalities"].count)
Data["Survivability"] = round(10 * (Data["TotalAboard"] - Data["Total Fatalities"]) / Data["TotalAboard"])

myList = Data['Operator']
UniqueNamesLists=[]
aa=0


def TestForUnique(value, labels, UniqueNamesLists,allValues):
  if len(UniqueNamesLists) != 0:
    for i in range(0, len(UniqueNamesLists)):
      sim = similar(ReplaceFrequent(value.lower(), labels[:20]), ReplaceFrequent(UniqueNamesLists[i].lower(), labels[:20]))
      if sim > 0.83 and sim <= 1:
        value = UniqueNamesLists[i]
        return value
  for i in range(0, len(allValues)):
    sim = similar(ReplaceFrequent(value, labels[:20]), ReplaceFrequent(allValues[i], labels[:20]))
    if sim > 0.83 and sim <= 1:
      UniqueNamesLists.append(allValues[i].lower())
      return
  UniqueNamesLists.append(value.lower())
  return


for k in range(len(myList)):
  tempList=[x for x in myList if x != myList[k]]
  str=TestForUnique(myList[k], labels[:20], UniqueNamesLists,tempList)
  if str is not None:
    myList[k]=str;
  print(len(UniqueNamesLists))
  if k%100==0:
    with open('newoperators.txt', 'w') as f:
      for item in UniqueNamesLists:
        f.write("%s\n" % item)
print('done')