# Stage model edits through gather steps

All model-edit data and Rhino-document directives enter `RhinoUrbanModelPipeline` through ordered `IRhinoDataGatherStep` instances, while pipeline configuration methods register execution steps only. This removes direct data and deletion injection, keeps validation and staging together at each operation boundary, and makes every caller follow the same preparation path without changing pipeline execution or failure behaviour.
