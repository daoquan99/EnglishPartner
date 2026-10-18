using EnglishLiveTutor.Domain.Lessons.Enums;
using EnglishLiveTutor.Domain.Pronunciation;
using Microsoft.EntityFrameworkCore;

namespace EnglishLiveTutor.Infrastructure.Persistence;

/// <summary>
/// Seeds the 10 canonical AI scoring rubric templates with descriptors for lesson exercises.
/// </summary>
public static class LessonRubricSeeder
{
    public static async Task SeedAsync(AppDbContext db)
    {
        // 1. LESSON_OPEN_CLOZE
        if (!await db.ScoringRubrics.AnyAsync(r => r.Code == "LESSON_OPEN_CLOZE"))
        {
            var rubric = new ScoringRubricEntity(
                code: "LESSON_OPEN_CLOZE",
                name: "Rubric Điền khuyết mở (Open Cloze)",
                description: "Đánh giá từ hoặc cụm từ học viên tự điền vào chỗ trống trong văn cảnh.",
                version: 1,
                assessmentType: "LessonExercise",
                isActive: true,
                assessmentScope: "LessonExercise",
                exerciseType: (int)ExerciseType.FillInTheBlanks,
                cefrLevel: "All",
                id: Guid.Parse("30000000-0000-0000-0000-000000000001"));

            rubric.AddCriterion(
                code: "CONTEXT_APPROPRIATENESS",
                name: "Phù hợp ngữ cảnh",
                description: "Từ điền phù hợp ý nghĩa, sắc thái và tính logic của văn cảnh.",
                weight: 60.00m,
                displayOrder: 1,
                descriptorLevel0: "Từ điền hoàn toàn vô nghĩa hoặc làm sai lệch nghiêm trọng ngữ cảnh.",
                descriptorLevel1: "Từ điền có liên quan mơ hồ nhưng không ăn khớp với ngữ cảnh của câu.",
                descriptorLevel2: "Từ điền hiểu được ý nhưng chưa phải là lựa chọn tự nhiên nhất trong văn cảnh.",
                descriptorLevel3: "Từ điền rất phù hợp với ngữ cảnh, truyền tải đúng ý đồ của câu.",
                descriptorLevel4: "Từ điền hoàn hảo, tự nhiên như người bản xứ, kết hợp collocations chuẩn xác.",
                id: Guid.Parse("30000000-0000-0000-0000-000000000101"));

            rubric.AddCriterion(
                code: "GRAMMATICAL_ACCURACY",
                name: "Độ chính xác ngữ pháp",
                description: "Đúng dạng từ (từ loại, chia thì động từ, số ít/nhiều).",
                weight: 40.00m,
                displayOrder: 2,
                descriptorLevel0: "Sai hoàn toàn về từ loại và ngữ pháp (ví dụ: cần danh từ lại điền động từ nguyên mẫu).",
                descriptorLevel1: "Đúng từ gốc nhưng sai dạng thức ngữ pháp nghiêm trọng (chia thì, tiền tố/hậu tố).",
                descriptorLevel2: "Đúng từ loại nhưng có lỗi nhỏ về chính tả hoặc dạng số ít/nhiều.",
                descriptorLevel3: "Đúng ngữ pháp và dạng từ, cấu trúc câu chuẩn xác.",
                descriptorLevel4: "Chuẩn xác tuyệt đối về ngữ pháp, hình thái học và chính tả.",
                id: Guid.Parse("30000000-0000-0000-0000-000000000102"));

            rubric.ValidateWeights();
            db.ScoringRubrics.Add(rubric);
        }

        // 2. LESSON_SENTENCE_UNSCRAMBLE
        if (!await db.ScoringRubrics.AnyAsync(r => r.Code == "LESSON_SENTENCE_UNSCRAMBLE"))
        {
            var rubric = new ScoringRubricEntity(
                code: "LESSON_SENTENCE_UNSCRAMBLE",
                name: "Rubric Sắp xếp câu mở (Sentence Unscramble)",
                description: "Đánh giá câu hoàn chỉnh do học viên sắp xếp từ các thành phần cho sẵn khi có nhiều cách viết.",
                version: 1,
                assessmentType: "LessonExercise",
                isActive: true,
                assessmentScope: "LessonExercise",
                exerciseType: (int)ExerciseType.SentenceUnscramble,
                cefrLevel: "All",
                id: Guid.Parse("30000000-0000-0000-0000-000000000002"));

            rubric.AddCriterion(
                code: "TOKEN_USAGE",
                name: "Dùng đúng thành phần",
                description: "Sử dụng đầy đủ và chính xác các token đã cho mà không bỏ sót hoặc thêm từ.",
                weight: 40.00m,
                displayOrder: 1,
                descriptorLevel0: "Bỏ sót phần lớn các từ hoặc tự tiện thêm nhiều từ ngoài danh sách.",
                descriptorLevel1: "Bỏ sót 2-3 từ quan trọng làm mất thành phần nòng cốt của câu.",
                descriptorLevel2: "Bỏ sót 1 từ phụ (mạo từ, giới từ) hoặc dùng lặp từ.",
                descriptorLevel3: "Sử dụng đầy đủ tất cả các token được cung cấp.",
                descriptorLevel4: "Sử dụng trọn vẹn, chính xác 100% các token với trật tự tối ưu.",
                id: Guid.Parse("30000000-0000-0000-0000-000000000201"));

            rubric.AddCriterion(
                code: "GRAMMATICAL_STRUCTURE",
                name: "Cấu trúc ngữ pháp",
                description: "Trật tự từ và cấu trúc cú pháp tuân thủ ngữ pháp tiếng Anh.",
                weight: 40.00m,
                displayOrder: 2,
                descriptorLevel0: "Trật tự từ lộn xộn, không tạo thành một mệnh đề hay câu có nghĩa.",
                descriptorLevel1: "Sai cấu trúc ngữ pháp cơ bản (chủ ngữ - vị ngữ sai trật tự trầm trọng).",
                descriptorLevel2: "Đúng cấu trúc chính nhưng đặt sai vị trí trạng từ hoặc mệnh đề phụ.",
                descriptorLevel3: "Cấu trúc ngữ pháp đúng chuẩn, trật tự từ tự nhiên.",
                descriptorLevel4: "Ngữ pháp hoàn hảo, xử lý xuất sắc các cấu trúc phức hoặc nhấn mạnh.",
                id: Guid.Parse("30000000-0000-0000-0000-000000000202"));

            rubric.AddCriterion(
                code: "SEMANTIC_COHERENCE",
                name: "Mạch lạc ngữ nghĩa",
                description: "Câu diễn đạt ý tưởng rõ ràng, tự nhiên và logic.",
                weight: 20.00m,
                displayOrder: 3,
                descriptorLevel0: "Câu vô nghĩa hoặc diễn đạt tối nghĩa không thể hiểu.",
                descriptorLevel1: "Ý nghĩa gượng gạo, người đọc phải đoán nghĩa.",
                descriptorLevel2: "Ý nghĩa tương đối rõ ràng dù hành văn hơi cứng.",
                descriptorLevel3: "Ý nghĩa rõ ràng, mạch lạc, tự nhiên.",
                descriptorLevel4: "Ý nghĩa sâu sắc, mạch lạc, phong cách diễn đạt mượt mà.",
                id: Guid.Parse("30000000-0000-0000-0000-000000000203"));

            rubric.ValidateWeights();
            db.ScoringRubrics.Add(rubric);
        }

        // 3. LESSON_ERROR_CORRECTION
        if (!await db.ScoringRubrics.AnyAsync(r => r.Code == "LESSON_ERROR_CORRECTION"))
        {
            var rubric = new ScoringRubricEntity(
                code: "LESSON_ERROR_CORRECTION",
                name: "Rubric Tìm và sửa lỗi sai (Error Correction)",
                description: "Đánh giá câu sửa lỗi của học viên khi sửa theo nhiều cách diễn đạt khác nhau.",
                version: 1,
                assessmentType: "LessonExercise",
                isActive: true,
                assessmentScope: "LessonExercise",
                exerciseType: (int)ExerciseType.ErrorCorrection,
                cefrLevel: "All",
                id: Guid.Parse("30000000-0000-0000-0000-000000000003"));

            rubric.AddCriterion(
                code: "TARGET_ERROR_FIX",
                name: "Sửa đúng lỗi mục tiêu",
                description: "Phát hiện và sửa chính xác lỗi ngữ pháp hoặc dùng từ mục tiêu.",
                weight: 40.00m,
                displayOrder: 1,
                descriptorLevel0: "Không phát hiện được lỗi sai mục tiêu hoặc sửa thành một lỗi sai khác nặng hơn.",
                descriptorLevel1: "Chỉ ra được vị trí lỗi nhưng sửa chưa đúng dạng ngữ pháp.",
                descriptorLevel2: "Sửa được lỗi mục tiêu nhưng câu sau khi sửa còn một vài lỗi phụ.",
                descriptorLevel3: "Sửa chính xác hoàn toàn các lỗi sai mục tiêu.",
                descriptorLevel4: "Sửa lỗi xuất sắc, diễn đạt lại câu ngắn gọn, chuẩn xác và tự nhiên.",
                id: Guid.Parse("30000000-0000-0000-0000-000000000301"));

            rubric.AddCriterion(
                code: "MEANING_PRESERVATION",
                name: "Bảo toàn ý nghĩa",
                description: "Không làm thay đổi thông điệp và ý định diễn đạt của câu gốc.",
                weight: 30.00m,
                displayOrder: 2,
                descriptorLevel0: "Thay đổi hoàn toàn ý nghĩa của câu gốc.",
                descriptorLevel1: "Làm mất đi phần lớn thông điệp hoặc thêm thắt ý không có trong câu gốc.",
                descriptorLevel2: "Bảo toàn ý chính nhưng sắc thái biểu cảm bị thay đổi.",
                descriptorLevel3: "Bảo toàn nguyên vẹn ý nghĩa và ngữ cảnh của câu gốc.",
                descriptorLevel4: "Giữ trọn vẹn thông điệp gốc với sắc thái biểu đạt tinh tế.",
                id: Guid.Parse("30000000-0000-0000-0000-000000000302"));

            rubric.AddCriterion(
                code: "OVERALL_GRAMMAR",
                name: "Ngữ pháp tổng thể",
                description: "Câu sau khi sửa đúng ngữ pháp, chính tả và không phát sinh lỗi mới.",
                weight: 30.00m,
                displayOrder: 3,
                descriptorLevel0: "Câu sau khi sửa mắc nhiều lỗi ngữ pháp mới.",
                descriptorLevel1: "Câu có 2-3 lỗi sai ngữ pháp hoặc chính tả mới.",
                descriptorLevel2: "Câu chỉ còn 1 lỗi nhỏ về dấu câu hoặc viết hoa.",
                descriptorLevel3: "Câu sau khi sửa hoàn toàn chuẩn xác về ngữ pháp và chính tả.",
                descriptorLevel4: "Ngữ pháp hoàn hảo, không có bất kỳ sai sót nào.",
                id: Guid.Parse("30000000-0000-0000-0000-000000000303"));

            rubric.ValidateWeights();
            db.ScoringRubrics.Add(rubric);
        }

        // 4. LESSON_TRANSLATION
        if (!await db.ScoringRubrics.AnyAsync(r => r.Code == "LESSON_TRANSLATION"))
        {
            var rubric = new ScoringRubricEntity(
                code: "LESSON_TRANSLATION",
                name: "Rubric Dịch câu / đoạn ngắn (Translation)",
                description: "Đánh giá bản dịch xuôi / ngược câu hoặc đoạn văn bản tiếng Anh.",
                version: 1,
                assessmentType: "LessonExercise",
                isActive: true,
                assessmentScope: "LessonExercise",
                exerciseType: (int)ExerciseType.Translation,
                cefrLevel: "All",
                id: Guid.Parse("30000000-0000-0000-0000-000000000004"));

            rubric.AddCriterion(
                code: "MEANING_FIDELITY",
                name: "Đúng nghĩa gốc",
                description: "Truyền tải chính xác, đầy đủ ý nghĩa và thông điệp của câu nguồn.",
                weight: 50.00m,
                displayOrder: 1,
                descriptorLevel0: "Dịch sai hoàn toàn ý nghĩa câu gốc hoặc dịch lạc đề.",
                descriptorLevel1: "Dịch sai các từ khóa chính, làm biến dạng thông điệp câu gốc.",
                descriptorLevel2: "Truyền tải được ý chính nhưng còn sót hoặc hiểu sai một ý phụ.",
                descriptorLevel3: "Truyền tải đầy đủ và chính xác ý nghĩa của câu nguồn.",
                descriptorLevel4: "Chính xác hoàn toàn nghĩa gốc, tái hiện tinh tế sắc thái biểu cảm.",
                id: Guid.Parse("30000000-0000-0000-0000-000000000401"));

            rubric.AddCriterion(
                code: "COMPLETENESS",
                name: "Đầy đủ thông tin",
                description: "Không bỏ sót các chi tiết, trạng từ hoặc yếu tố bổ trợ quan trọng.",
                weight: 20.00m,
                displayOrder: 2,
                descriptorLevel0: "Bỏ sót hơn một nửa câu hoặc chỉ dịch được vài từ rời rạc.",
                descriptorLevel1: "Bỏ sót các thành phần ngữ pháp chính (chủ ngữ, vị ngữ, tân ngữ).",
                descriptorLevel2: "Bỏ sót trạng từ chỉ thời gian, nơi chốn hoặc từ nhấn mạnh.",
                descriptorLevel3: "Dịch đầy đủ tất cả các thành phần thông tin của câu gốc.",
                descriptorLevel4: "Dịch trọn vẹn, không thêm thắt hay bỏ sót bất kỳ chi tiết nào.",
                id: Guid.Parse("30000000-0000-0000-0000-000000000402"));

            rubric.AddCriterion(
                code: "GRAMMATICAL_ACCURACY",
                name: "Chính xác ngữ pháp",
                description: "Cấu trúc câu dịch đúng quy tắc ngữ pháp của ngôn ngữ đích.",
                weight: 15.00m,
                displayOrder: 3,
                descriptorLevel0: "Cấu trúc câu đích vỡ vụn, sai ngữ pháp hoàn toàn.",
                descriptorLevel1: "Nhiều lỗi ngữ pháp cơ bản (chia thì, trật tự từ, giới từ).",
                descriptorLevel2: "Ngữ pháp câu tương đối tốt, chỉ có 1 lỗi nhỏ không gây hiểu nhầm.",
                descriptorLevel3: "Cấu trúc ngữ pháp hoàn toàn đúng chuẩn mực.",
                descriptorLevel4: "Ngữ pháp hoàn hảo, kết hợp cấu trúc phong phú và chính xác tuyệt đối.",
                id: Guid.Parse("30000000-0000-0000-0000-000000000403"));

            rubric.AddCriterion(
                code: "NATURAL_EXPRESSION",
                name: "Diễn đạt tự nhiên",
                description: "Hành văn tự nhiên, chuẩn phong cách bản ngữ, không bị dịch gượng.",
                weight: 15.00m,
                displayOrder: 4,
                descriptorLevel0: "Bản dịch word-by-word máy móc, vô nghĩa.",
                descriptorLevel1: "Hành văn rất gượng gạo, đậm mùi dịch thô ráp.",
                descriptorLevel2: "Hành văn chấp nhận được nhưng chưa thực sự tự nhiên.",
                descriptorLevel3: "Diễn đạt mượt mà, dùng từ tự nhiên đúng văn phong.",
                descriptorLevel4: "Diễn đạt tự nhiên, uyển chuyển như câu viết bởi người bản xứ.",
                id: Guid.Parse("30000000-0000-0000-0000-000000000404"));

            rubric.ValidateWeights();
            db.ScoringRubrics.Add(rubric);
        }

        // 5. LESSON_SHORT_ANSWER
        if (!await db.ScoringRubrics.AnyAsync(r => r.Code == "LESSON_SHORT_ANSWER"))
        {
            var rubric = new ScoringRubricEntity(
                code: "LESSON_SHORT_ANSWER",
                name: "Rubric Trả lời ngắn Đọc/Nghe hiểu (Short Answer Comprehension)",
                description: "Đánh giá câu trả lời mở ngắn cho các bài Đọc hiểu và Nghe hiểu.",
                version: 1,
                assessmentType: "LessonExercise",
                isActive: true,
                assessmentScope: "LessonExercise",
                exerciseType: null,
                subQuestionKind: "ShortAnswer",
                applicableExerciseTypes: "ReadingComprehension,ListeningComprehension",
                cefrLevel: "All",
                id: Guid.Parse("30000000-0000-0000-0000-000000000005"));

            rubric.AddCriterion(
                code: "CONTENT_ACCURACY",
                name: "Đúng trọng tâm nội dung",
                description: "Trả lời trực diện vào câu hỏi dựa trên thông tin bài đọc/nghe.",
                weight: 60.00m,
                displayOrder: 1,
                descriptorLevel0: "Trả lời hoàn toàn lạc đề hoặc không có thông tin từ bài đọc/nghe.",
                descriptorLevel1: "Nhắc đến chủ đề nhưng đưa ra thông tin sai lệch so với bài.",
                descriptorLevel2: "Trả lời được một phần thông tin, còn thiếu ý trọng tâm.",
                descriptorLevel3: "Trả lời đúng trọng tâm và chính xác theo dữ liệu bài đọc/nghe.",
                descriptorLevel4: "Trả lời cực kỳ chính xác, ngắn gọn, súc tích và bám sát câu hỏi.",
                id: Guid.Parse("30000000-0000-0000-0000-000000000501"));

            rubric.AddCriterion(
                code: "EVIDENCE_SUPPORT",
                name: "Dẫn chứng / Đầy đủ ý",
                description: "Đưa ra đủ các lý do hoặc minh chứng cần thiết từ văn bản.",
                weight: 25.00m,
                displayOrder: 2,
                descriptorLevel0: "Không có bất kỳ dẫn chứng hay lý lẽ nào.",
                descriptorLevel1: "Dẫn chứng sai hoặc không liên quan đến câu trả lời.",
                descriptorLevel2: "Có dẫn chứng nhưng chưa đủ thuyết phục hoặc còn thiếu ý phụ.",
                descriptorLevel3: "Cung cấp đầy đủ dẫn chứng và chi tiết hỗ trợ từ bài.",
                descriptorLevel4: "Dẫn chứng sắc bén, nêu bật chính xác thông tin then chốt.",
                id: Guid.Parse("30000000-0000-0000-0000-000000000502"));

            rubric.AddCriterion(
                code: "EXPRESSION_CLARITY",
                name: "Rõ ràng, mạch lạc",
                description: "Câu trả lời dễ hiểu, ít lỗi ngữ pháp gây hiểu lầm.",
                weight: 15.00m,
                displayOrder: 3,
                descriptorLevel0: "Diễn đạt tối nghĩa, lỗi câu nặng nề.",
                descriptorLevel1: "Nhiều lỗi ngữ pháp và từ vựng gây khó khăn cho việc hiểu.",
                descriptorLevel2: "Hiểu được ý chính dù còn lỗi ngữ pháp/chính tả nhỏ.",
                descriptorLevel3: "Diễn đạt rõ ràng, ngữ pháp chuẩn, từ ngữ phù hợp.",
                descriptorLevel4: "Trình bày mạch lạc, câu văn trau chuốt và chuẩn mực.",
                id: Guid.Parse("30000000-0000-0000-0000-000000000503"));

            rubric.ValidateWeights();
            db.ScoringRubrics.Add(rubric);
        }

        // 6. LESSON_WRITING
        if (!await db.ScoringRubrics.AnyAsync(r => r.Code == "LESSON_WRITING"))
        {
            var rubric = new ScoringRubricEntity(
                code: "LESSON_WRITING",
                name: "Rubric Luyện viết đoạn văn (Writing Practice)",
                description: "Đánh giá bài viết đoạn văn hoặc email theo yêu cầu đề bài.",
                version: 1,
                assessmentType: "LessonExercise",
                isActive: true,
                assessmentScope: "LessonExercise",
                exerciseType: (int)ExerciseType.WritingPractice,
                cefrLevel: "All",
                id: Guid.Parse("30000000-0000-0000-0000-000000000006"));

            rubric.AddCriterion(
                code: "TASK_ACHIEVEMENT",
                name: "Hoàn thành yêu cầu đề bài",
                description: "Đạt độ dài, đúng chủ đề và đáp ứng toàn bộ các ý bắt buộc.",
                weight: 35.00m,
                displayOrder: 1,
                descriptorLevel0: "Lạc đề hoàn toàn hoặc bài viết quá ngắn (dưới 30% độ dài yêu cầu).",
                descriptorLevel1: "Chỉ đề cập sơ sài một phần chủ đề, bỏ sót hầu hết ý bắt buộc.",
                descriptorLevel2: "Đáp ứng được một số ý chính nhưng bài viết chưa trọn vẹn.",
                descriptorLevel3: "Hoàn thành đầy đủ các yêu cầu đề bài và đạt giới hạn từ quy định.",
                descriptorLevel4: "Phát triển ý tưởng xuất sắc, luận điểm sâu sắc và đáp ứng trọn vẹn đề bài.",
                id: Guid.Parse("30000000-0000-0000-0000-000000000601"));

            rubric.AddCriterion(
                code: "COHERENCE_COHESION",
                name: "Mạch lạc và liên kết",
                description: "Bố cục rõ ràng, sử dụng từ nối và liên kết câu hợp lý.",
                weight: 20.00m,
                displayOrder: 2,
                descriptorLevel0: "Ý tưởng rời rạc, không có bố cục hoặc liên kết logic.",
                descriptorLevel1: "Bố cục yếu, sử dụng từ nối sai hoặc rất lặp từ.",
                descriptorLevel2: "Có cấu trúc cơ bản nhưng chuyển ý còn gượng gạo.",
                descriptorLevel3: "Bố cục rõ ràng, phân đoạn hợp lý, từ nối tự nhiên.",
                descriptorLevel4: "Mạch lạc tuyệt vời, chuyển ý uyển chuyển và lập luận chặt chẽ.",
                id: Guid.Parse("30000000-0000-0000-0000-000000000602"));

            rubric.AddCriterion(
                code: "GRAMMATICAL_RANGE",
                name: "Độ phong phú ngữ pháp",
                description: "Kết hợp câu đơn và câu phức, mức độ chính xác của cấu trúc.",
                weight: 25.00m,
                displayOrder: 3,
                descriptorLevel0: "Chỉ dùng câu cụt hoặc sai ngữ pháp trong hầu hết các câu.",
                descriptorLevel1: "Chỉ dùng câu đơn giản, nhiều lỗi ngữ pháp cơ bản.",
                descriptorLevel2: "Có nỗ lực dùng câu phức nhưng đôi khi còn mắc lỗi cấu trúc.",
                descriptorLevel3: "Sử dụng linh hoạt câu đơn và câu phức, độ chính xác cao.",
                descriptorLevel4: "Làm chủ đa dạng cấu trúc ngữ pháp nâng cao với độ chuẩn xác tuyệt đối.",
                id: Guid.Parse("30000000-0000-0000-0000-000000000603"));

            rubric.AddCriterion(
                code: "LEXICAL_RESOURCE",
                name: "Vốn từ vựng",
                description: "Sử dụng từ vựng đa dạng, đúng ngữ cảnh và collocations phù hợp.",
                weight: 20.00m,
                displayOrder: 4,
                descriptorLevel0: "Vốn từ cực kỳ nghèo nàn, dùng từ sai nghĩa trầm trọng.",
                descriptorLevel1: "Lặp từ liên tục, dùng từ vựng quá sơ cấp so với yêu cầu level.",
                descriptorLevel2: "Vốn từ đủ để diễn đạt ý nhưng chưa có collocations hay từ vựng theo chủ đề.",
                descriptorLevel3: "Sử dụng tốt từ vựng theo chủ đề, đúng collocations.",
                descriptorLevel4: "Vốn từ vựng phong phú, sử dụng tinh tế các thành ngữ và từ vựng nâng cao.",
                id: Guid.Parse("30000000-0000-0000-0000-000000000604"));

            rubric.ValidateWeights();
            db.ScoringRubrics.Add(rubric);
        }

        // 7. LESSON_SPEAKING
        if (!await db.ScoringRubrics.AnyAsync(r => r.Code == "LESSON_SPEAKING"))
        {
            var rubric = new ScoringRubricEntity(
                code: "LESSON_SPEAKING",
                name: "Rubric Luyện nói (Speaking Practice)",
                description: "Đánh giá bài nói độc thoại theo chủ đề của học viên.",
                version: 1,
                assessmentType: "LessonExercise",
                isActive: true,
                assessmentScope: "LessonExercise",
                exerciseType: (int)ExerciseType.SpeakingPractice,
                cefrLevel: "All",
                id: Guid.Parse("30000000-0000-0000-0000-000000000007"));

            rubric.AddCriterion(
                code: "TASK_COMPLETION",
                name: "Hoàn thành nhiệm vụ nói",
                description: "Trả lời đầy đủ các gợi ý và đạt thời lượng mục tiêu.",
                weight: 30.00m,
                displayOrder: 1,
                descriptorLevel0: "Nói dưới 10 giây hoặc hoàn toàn không liên quan đến chủ đề.",
                descriptorLevel1: "Chỉ nói được vài câu đơn lẻ, bỏ qua hầu hết gợi ý.",
                descriptorLevel2: "Đề cập được các ý cơ bản nhưng bài nói bị ngắt quãng hoặc chưa đủ dài.",
                descriptorLevel3: "Hoàn thành bài nói đúng thời lượng và giải quyết tốt các câu hỏi gợi ý.",
                descriptorLevel4: "Bài nói đầy đủ, sâu sắc, mở rộng ý tưởng tự nhiên và cuốn hút.",
                id: Guid.Parse("30000000-0000-0000-0000-000000000701"));

            rubric.AddCriterion(
                code: "GRAMMATICAL_ACCURACY",
                name: "Ngữ pháp câu nói",
                description: "Độ chuẩn xác ngữ pháp trong lời nói tự nhiên.",
                weight: 20.00m,
                displayOrder: 2,
                descriptorLevel0: "Sai ngữ pháp liên tục làm người nghe không thể hiểu.",
                descriptorLevel1: "Mắc nhiều lỗi thì, chia động từ và thiếu chủ ngữ/vị ngữ.",
                descriptorLevel2: "Ngữ pháp ở mức chấp nhận được, có một số lỗi sai khi dùng câu dài.",
                descriptorLevel3: "Kiểm soát tốt ngữ pháp, cấu trúc câu nói chuẩn xác.",
                descriptorLevel4: "Ngữ pháp hoàn hảo, kết hợp cấu trúc phong phú không có lỗi sai đáng kể.",
                id: Guid.Parse("30000000-0000-0000-0000-000000000702"));

            rubric.AddCriterion(
                code: "LEXICAL_RESOURCE",
                name: "Vốn từ sử dụng",
                description: "Sử dụng từ ngữ phù hợp chủ đề và đa dạng.",
                weight: 20.00m,
                displayOrder: 3,
                descriptorLevel0: "Từ vựng quá hạn chế, không đủ để diễn đạt ý cơ bản.",
                descriptorLevel1: "Dùng từ sai ngữ cảnh nhiều lần, vốn từ đơn điệu.",
                descriptorLevel2: "Từ vựng đủ dùng cho giao tiếp hàng ngày nhưng thiếu từ theo chủ đề.",
                descriptorLevel3: "Sử dụng từ vựng phong phú, chuẩn xác theo chủ đề bài học.",
                descriptorLevel4: "Vốn từ vựng rất linh hoạt, dùng collocations chuẩn xác và tự nhiên.",
                id: Guid.Parse("30000000-0000-0000-0000-000000000703"));

            rubric.AddCriterion(
                code: "PRONUNCIATION_INTELLIGIBILITY",
                name: "Phát âm dễ hiểu",
                description: "Phát âm âm tiết, trọng âm và ngữ điệu tự nhiên, người nghe hiểu ngay.",
                weight: 15.00m,
                displayOrder: 4,
                descriptorLevel0: "Phát âm sai hầu hết các từ, người nghe không thể nhận diện được từ.",
                descriptorLevel1: "Phát âm sai âm cuối và trọng âm thường xuyên, gây hiểu lầm.",
                descriptorLevel2: "Phát âm ở mức trung bình, người nghe cần tập trung mới hiểu được.",
                descriptorLevel3: "Phát âm rõ ràng, đúng trọng âm từ và ngữ điệu cơ bản.",
                descriptorLevel4: "Phát âm chuẩn xác, ngữ điệu tự nhiên, nối âm mượt mà như người bản xứ.",
                id: Guid.Parse("30000000-0000-0000-0000-000000000704"));

            rubric.AddCriterion(
                code: "FLUENCY_COHERENCE",
                name: "Độ trôi chảy & mạch lạc",
                description: "Nói trôi chảy, tốc độ hợp lý, ít ngập ngừng kéo dài.",
                weight: 15.00m,
                displayOrder: 5,
                descriptorLevel0: "Ngập ngừng liên tục, ngắt quãng quá lâu không thể duy trì lời nói.",
                descriptorLevel1: "Nói từng từ rời rạc, thường xuyên dừng lại để tìm từ.",
                descriptorLevel2: "Có một số đoạn ngập ngừng nhưng vẫn duy trì được mạch bài nói.",
                descriptorLevel3: "Nói trôi chảy, tốc độ đều đặn, ngắt nghỉ đúng chỗ.",
                descriptorLevel4: "Rất trôi chảy, tự tin, diễn đạt liên tục với nhịp điệu sinh động.",
                id: Guid.Parse("30000000-0000-0000-0000-000000000705"));

            rubric.ValidateWeights();
            db.ScoringRubrics.Add(rubric);
        }

        // 8. LESSON_SHADOWING
        if (!await db.ScoringRubrics.AnyAsync(r => r.Code == "LESSON_SHADOWING"))
        {
            var rubric = new ScoringRubricEntity(
                code: "LESSON_SHADOWING",
                name: "Rubric Nói nhại theo audio (Shadowing Practice)",
                description: "Đánh giá mức độ bám sát audio mẫu về phát âm, nhịp điệu và ngữ điệu.",
                version: 1,
                assessmentType: "LessonExercise",
                isActive: true,
                assessmentScope: "LessonExercise",
                exerciseType: (int)ExerciseType.Shadowing,
                cefrLevel: "All",
                id: Guid.Parse("30000000-0000-0000-0000-000000000008"));

            rubric.AddCriterion(
                code: "CONTENT_ACCURACY",
                name: "Đúng nội dung câu mẫu",
                description: "Đọc đúng và đủ tất cả các từ trong văn bản mẫu không bỏ sót hoặc thêm từ.",
                weight: 40.00m,
                displayOrder: 1,
                descriptorLevel0: "Bỏ sót quá 50% số từ hoặc đọc sai lệch nội dung.",
                descriptorLevel1: "Bỏ sót một số từ quan trọng hoặc vấp váp nhiều từ.",
                descriptorLevel2: "Đọc được hầu hết các từ, có 1-2 từ đọc lướt bị mất âm.",
                descriptorLevel3: "Đọc chính xác và đầy đủ toàn bộ các từ trong văn bản.",
                descriptorLevel4: "Khớp hoàn hảo 100% nội dung văn bản mẫu không có bất kỳ sai sót nào.",
                id: Guid.Parse("30000000-0000-0000-0000-000000000801"));

            rubric.AddCriterion(
                code: "PRONUNCIATION_ACCURACY",
                name: "Phát âm chuẩn xác",
                description: "Phát âm đúng từng âm vị, âm cuối và nối âm theo audio mẫu.",
                weight: 35.00m,
                displayOrder: 2,
                descriptorLevel0: "Phát âm sai lệch nghiêm trọng so với phát âm mẫu.",
                descriptorLevel1: "Mất âm cuối (ending sounds) và phát âm sai nhiều nguyên âm.",
                descriptorLevel2: "Phát âm tương đối tốt, còn cứng ở các âm nối hoặc biến âm.",
                descriptorLevel3: "Phát âm chuẩn xác, rõ âm cuối và bắt chước tốt các liên kết âm.",
                descriptorLevel4: "Phát âm tinh tế, tái hiện hoàn hảo giọng đọc chuẩn của file mẫu.",
                id: Guid.Parse("30000000-0000-0000-0000-000000000802"));

            rubric.AddCriterion(
                code: "RHYTHM_AND_PACING",
                name: "Nhịp điệu và tốc độ",
                description: "Tốc độ nói, ngữ điệu lên xuống và ngắt nghỉ đồng bộ với file audio.",
                weight: 25.00m,
                displayOrder: 3,
                descriptorLevel0: "Tốc độ nói quá nhanh hoặc quá chậm, hoàn toàn lệch nhịp với audio.",
                descriptorLevel1: "Ngắt nghỉ sai chỗ, ngữ điệu phẳng lặng không có cảm xúc.",
                descriptorLevel2: "Bám được nhịp cơ bản nhưng đôi lúc bị đuối hoặc chậm hơn audio mẫu.",
                descriptorLevel3: "Nhịp điệu tốt, tốc độ và ngữ điệu đồng bộ cao với audio.",
                descriptorLevel4: "Hòa nhịp xuất sắc, tái hiện hoàn hảo nhịp điệu và cảm xúc của người nói mẫu.",
                id: Guid.Parse("30000000-0000-0000-0000-000000000803"));

            rubric.ValidateWeights();
            db.ScoringRubrics.Add(rubric);
        }

        // 9. LESSON_CONVERSATION_TEXT
        if (!await db.ScoringRubrics.AnyAsync(r => r.Code == "LESSON_CONVERSATION_TEXT"))
        {
            var rubric = new ScoringRubricEntity(
                code: "LESSON_CONVERSATION_TEXT",
                name: "Rubric Hội thoại tình huống qua tin nhắn (Conversation Role-Play Text)",
                description: "Đánh giá khả năng tương tác và hoàn thành mục tiêu giao tiếp qua văn bản.",
                version: 1,
                assessmentType: "LessonExercise",
                isActive: true,
                assessmentScope: "LessonExercise",
                exerciseType: (int)ExerciseType.ConversationRolePlay,
                cefrLevel: "All",
                id: Guid.Parse("30000000-0000-0000-0000-000000000009"));

            rubric.AddCriterion(
                code: "TASK_SCENARIO_COMPLETION",
                name: "Hoàn thành tình huống",
                description: "Đạt được tất cả các mục tiêu giao tiếp đặt ra trong kịch bản.",
                weight: 40.00m,
                displayOrder: 1,
                descriptorLevel0: "Không đạt được mục tiêu nào hoặc trò chuyện hoàn toàn sai kịch bản.",
                descriptorLevel1: "Chỉ đạt được 1 mục tiêu nhỏ, dừng cuộc hội thoại giữa chừng.",
                descriptorLevel2: "Đạt được phần lớn mục tiêu nhưng còn thiếu sót một tình huống phụ.",
                descriptorLevel3: "Hoàn thành xuất sắc tất cả các mục tiêu giao tiếp của kịch bản.",
                descriptorLevel4: "Xử lý tình huống thông minh, khéo léo và trọn vẹn mọi mục tiêu giao tiếp.",
                id: Guid.Parse("30000000-0000-0000-0000-000000000901"));

            rubric.AddCriterion(
                code: "RESPONSIVENESS_INTERACTION",
                name: "Tương tác và phản hồi",
                description: "Hiểu câu hỏi và đáp lời logic, tự nhiên với từng lượt nói của AI.",
                weight: 30.00m,
                displayOrder: 2,
                descriptorLevel0: "Phản hồi vô nghĩa, không trả lời đúng câu hỏi của đối phương.",
                descriptorLevel1: "Phản hồi rời rạc, chỉ đưa ra câu trả lời cụt lủn không duy trì đối thoại.",
                descriptorLevel2: "Trả lời được nhưng phản xạ chậm, cách chuyển lượt chưa linh hoạt.",
                descriptorLevel3: "Tương tác tốt, hiểu đúng ngữ cảnh và đối đáp tự nhiên.",
                descriptorLevel4: "Tương tác cực kỳ mượt mà, chủ động dẫn dắt và phản hồi xuất sắc.",
                id: Guid.Parse("30000000-0000-0000-0000-000000000902"));

            rubric.AddCriterion(
                code: "LANGUAGE_QUALITY",
                name: "Chất lượng ngôn ngữ",
                description: "Ngữ pháp, từ vựng phong phú, lịch sự và phù hợp với vai diễn.",
                weight: 30.00m,
                displayOrder: 3,
                descriptorLevel0: "Nhiều lỗi ngữ pháp nặng nề, từ ngữ khiếm nhã hoặc không phù hợp vai diễn.",
                descriptorLevel1: "Vốn từ nghèo nàn, lặp cấu trúc liên tục.",
                descriptorLevel2: "Ngôn ngữ phù hợp ở mức cơ bản, đôi khi dùng từ chưa chuẩn phong cách.",
                descriptorLevel3: "Ngữ pháp chuẩn xác, từ vựng lịch sự, phù hợp hoàn hảo với vai diễn.",
                descriptorLevel4: "Ngôn ngữ phong phú, chuẩn mực ngoại giao và phong thái giao tiếp tuyệt vời.",
                id: Guid.Parse("30000000-0000-0000-0000-000000000903"));

            rubric.ValidateWeights();
            db.ScoringRubrics.Add(rubric);
        }

        // 10. LESSON_CONVERSATION_AUDIO
        if (!await db.ScoringRubrics.AnyAsync(r => r.Code == "LESSON_CONVERSATION_AUDIO"))
        {
            var rubric = new ScoringRubricEntity(
                code: "LESSON_CONVERSATION_AUDIO",
                name: "Rubric Hội thoại tình huống qua giọng nói (Conversation Role-Play Audio)",
                description: "Đánh giá khả năng tương tác trực tiếp bằng giọng nói trong tình huống thực tế.",
                version: 1,
                assessmentType: "LessonExercise",
                isActive: true,
                assessmentScope: "LessonExercise",
                exerciseType: (int)ExerciseType.ConversationRolePlay,
                cefrLevel: "All",
                id: Guid.Parse("30000000-0000-0000-0000-000000000010"));

            rubric.AddCriterion(
                code: "TASK_SCENARIO_COMPLETION",
                name: "Hoàn thành tình huống",
                description: "Đạt các mục tiêu giao tiếp trong tình huống thực tế.",
                weight: 35.00m,
                displayOrder: 1,
                descriptorLevel0: "Không đạt được mục tiêu nào trong giao tiếp.",
                descriptorLevel1: "Đạt mục tiêu hạn chế, cuộc hội thoại đứt quãng sớm.",
                descriptorLevel2: "Đạt được hầu hết mục tiêu của tình huống giao tiếp.",
                descriptorLevel3: "Hoàn thành đầy đủ tất cả các mục tiêu giao tiếp.",
                descriptorLevel4: "Hoàn thành xuất sắc, giải quyết tình huống nhanh nhạy và tinh tế.",
                id: Guid.Parse("30000000-0000-0000-0000-000000001001"));

            rubric.AddCriterion(
                code: "RESPONSIVENESS_INTERACTION",
                name: "Tương tác và phản hồi",
                description: "Phản hồi mượt mà, hiểu câu hỏi và đáp lời đúng trọng tâm.",
                weight: 25.00m,
                displayOrder: 2,
                descriptorLevel0: "Không hiểu câu hỏi của AI hoặc nói không liên quan.",
                descriptorLevel1: "Phản hồi lúng túng, mất nhiều thời gian để hiểu câu hỏi.",
                descriptorLevel2: "Phản hồi tương đối tốt dù đôi lúc cần AI nhắc lại.",
                descriptorLevel3: "Tương tác nhanh nhạy, đối đáp tự nhiên và tự tin.",
                descriptorLevel4: "Phản xạ giao tiếp nhạy bén, khả năng đàm thoại xuất sắc.",
                id: Guid.Parse("30000000-0000-0000-0000-000000001002"));

            rubric.AddCriterion(
                code: "LANGUAGE_QUALITY",
                name: "Chất lượng ngôn ngữ",
                description: "Ngữ pháp và từ vựng chuẩn mực, lịch sự.",
                weight: 25.00m,
                displayOrder: 3,
                descriptorLevel0: "Ngôn ngữ sai lệch nhiều, không thể hiện được ý muốn nói.",
                descriptorLevel1: "Từ vựng hạn chế, câu văn ngắc ngứ.",
                descriptorLevel2: "Ngữ pháp và từ vựng cơ bản, chấp nhận được trong giao tiếp thông thường.",
                descriptorLevel3: "Ngữ pháp chuẩn xác, từ vựng phong phú và chuẩn mực.",
                descriptorLevel4: "Diễn đạt đỉnh cao, từ ngữ tinh tế và chuẩn văn hóa giao tiếp.",
                id: Guid.Parse("30000000-0000-0000-0000-000000001003"));

            rubric.AddCriterion(
                code: "SPEECH_INTELLIGIBILITY",
                name: "Độ dễ hiểu của giọng nói",
                description: "Phát âm rõ ràng, âm lượng và ngữ điệu phù hợp, người nghe hiểu ngay.",
                weight: 15.00m,
                displayOrder: 4,
                descriptorLevel0: "Giọng nói không nghe rõ, phát âm sai làm người nghe không thể hiểu.",
                descriptorLevel1: "Phát âm ngọng hoặc sai trọng âm nhiều, gây khó hiểu.",
                descriptorLevel2: "Phát âm hiểu được, có một số chỗ phát âm chưa chuẩn.",
                descriptorLevel3: "Phát âm rõ ràng, âm lượng tốt, ngữ điệu tự nhiên.",
                descriptorLevel4: "Phát âm cực kỳ chuẩn xác, giọng nói truyền cảm và cuốn hút.",
                id: Guid.Parse("30000000-0000-0000-0000-000000001004"));

            rubric.ValidateWeights();
            db.ScoringRubrics.Add(rubric);
        }

        await db.SaveChangesAsync();
    }
}
