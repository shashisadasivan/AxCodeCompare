/// Created by: Shashi Sadasivan
/// Visit: shashidotnet.wordpress.com
 
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AxCodeCompare
{
    //TODO:
    /// <summary>
    /// Menu items never showed on on this.
    /// </summary>

    public class ModelElementE
    {
        public int ElementId { get; set; }
        public string Name { get; set; }
        public int ParentId { get; set; }
        public int RootId { get; set; }
        public string ElementType { get; set; }
        public string RootName { get; set; }

        public override string ToString()
        {
            return String.Format("{0},{1},{2}", ElementType, RootName, Name);
        }
    }

    public class CompareModels
    {
        private ConcurrentBag<ModelElementE> elementsE;
        private List<ElementType> _elementTypes;

        private List<int> _baseModels;
        private List<int> _modelsAffected;

        private CompareModels()
        {
            _baseModels = new List<int>();
            _modelsAffected = new List<int>();

            elementsE = new ConcurrentBag<ModelElementE>();
        }

        /// <summary>
        /// Creates an object with the given parameters
        /// </summary>
        /// <param name="baseModels">List of model id's whose code needs to be checked against other models</param>
        /// <param name="modelsAffected">List of models that may require a merge based on the baseModels</param>
        public CompareModels(List<int> baseModels, List<int> modelsAffected)
            :this()
        {
            _baseModels = baseModels;
            _modelsAffected = modelsAffected;
        }

        /// <summary>
        /// Creates an object with the given parameters
        /// </summary>
        /// <param name="baseModelsAsCSV">List of model id's whose code needs to be checked against other models in CSV</param>
        /// <param name="modelsAffectedAsCSV">List of models that may require a merge based on the baseModels</param>
        public CompareModels(string baseModelsAsCSV, string modelsAffectedAsCSV)
            : this()
        {
            var baselist = baseModelsAsCSV.Split(',');
            baselist.ToList().ForEach(s => _baseModels.Add(Int16.Parse(s)));

            var affList = modelsAffectedAsCSV.Split(',');
            affList.ToList().ForEach(s => _modelsAffected.Add(Int16.Parse(s)));
        }

        public List<string> Start()
        {
            // Get elements that exist in base model and Also in the modelsAffected
            using (DataTablesDataContext db = new DataTablesDataContext())
            {
                _elementTypes = (from e in db.ElementTypes select e).ToList();
                
                var modelElementDatas = from e in db.ModelElementDatas
                                        join medAdd in db.ModelElementDatas on e.ElementHandle equals medAdd.ElementHandle
                                        where _baseModels.Contains(e.ModelId)
                                            && _modelsAffected.Contains(medAdd.ModelId)
                                        select e;

                var elements = modelElementDatas.Select(e => e.ElementHandle).Distinct();
                var ee = elements.Where(x => x == 718722); // TODO: remove

                Console.WriteLine("Conflicts Count: " + elements.Count().ToString());

                // These are first level elements
                var modelElementsFirst =
                                    from e in db.ModelElements
                                    //where e.ParentHandle != 0 && modelElementDatas.Select(m => m.ParentHandle).Distinct().ToList().Contains(e.ParentHandle)
                                    where (elements.ToList().Contains(e.ElementHandle) && e.ParentHandle == 0)
                                    select e;
                Debug.WriteLine("Elements found: " + modelElementsFirst.Count().ToString());
                modelElementsFirst.AsParallel().ForAll(e => this.addModelElement(e));

                Debug.WriteLine("Elements Added");
                var modelElementsSecond =
                                    from e in db.ModelElements
                                    //where e.ParentHandle != 0 && modelElementDatas.Select(m => m.ParentHandle).Distinct().ToList().Contains(e.ParentHandle)
                                    where (elements.ToList().Contains(e.ElementHandle) && e.ParentHandle != 0)
                                    select e;
                var rootHandles = modelElementsSecond.Select(e => e.RootHandle).ToList();
                var modelElementsRoot =
                                    (from e in db.ModelElements
                                     where rootHandles.Contains(e.ElementHandle)
                                    select e).ToList();

                Debug.WriteLine("Elements found: " + modelElementsSecond.Count().ToString());
                modelElementsSecond.AsParallel()
                    .ForAll(e => 
                        this.addModelElement(e, modelElementsRoot.Where(r => r.ElementHandle == e.RootHandle).First())
                        );
                Debug.WriteLine("Elements added");


                List<string> lines = new List<string>();
                elementsE
                    .OrderBy(e => e.ElementType)
                    .ToList()
                    .ForEach(e => lines.Add(e.ToString()));

                return lines;
            }

        }

        public void addModelElement(ModelElement elementData, ModelElement elementRoot = null)
        {
            // This check is usually for classes / tables...etc. 
            // removing this if statement will not allow the menu items, security, SSRS reports to be added to the list
            if (elementData.ParentId != 0)
            {
                var x = elementsE.Where(e => e.ParentId == elementData.ParentHandle).FirstOrDefault();
                if (x != null)
                    return;
            }
            //if(String.IsNullOrEmpty(rootName))
            //{
            //    using (var db = new DataTablesDataContext())
            //    {
            //        var rootElement = (from e in db.ModelElements
            //                          where e.ParentId == elementData.ParentId
            //                          select e).FirstOrDefault();
            //        if(rootElement.ParentId != 0)
            //    }
            //}

            var elementType = _elementTypes.Where(e => e.ElementType1 == elementData.ElementType).FirstOrDefault();
            ModelElementE newElement = new ModelElementE()
            {
                ElementId = elementData.ElementHandle,
                ElementType = elementType != null ? elementType.ElementTypeName : String.Empty,
                Name = elementData.Name,
                ParentId = elementData.ParentId ?? 0,
                RootId = elementData.RootHandle,
                RootName = elementRoot != null ? elementRoot.Name : String.Empty
            };
            elementsE.Add(newElement);
        }

    }
}
